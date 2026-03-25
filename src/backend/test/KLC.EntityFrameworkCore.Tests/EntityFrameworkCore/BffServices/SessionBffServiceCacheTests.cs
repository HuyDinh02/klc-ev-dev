using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Fleets;
using KLC.Sessions;
using KLC.Stations;
using KLC.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using ActiveSessionDto = KLC.Driver.Services.ActiveSessionDto;

namespace KLC.BffServices;

/// <summary>
/// Tests for SessionBffService cache layer behavior.
/// Uses NSubstitute mocks for ICacheService to verify cache interactions.
/// </summary>
[Collection(KLCTestConsts.CollectionDefinitionName)]
public class SessionBffServiceCacheTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly SessionBffService _service;

    public SessionBffServiceCacheTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _cache = Substitute.For<ICacheService>();
        var fleetPolicyService = Substitute.For<IFleetChargingPolicyService>();
        fleetPolicyService.ValidateChargingAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new FleetChargingValidationResult(true));
        var logger = Substitute.For<ILogger<SessionBffService>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Wallet:MinBalanceToStart"] = "10000" })
            .Build();
        _service = new SessionBffService(_dbContext, _cache, fleetPolicyService, configuration, logger);
    }

    [Fact]
    public async Task GetActiveSession_Should_Return_Cached_Session_On_Hit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var cachedSession = new ActiveSessionDto
        {
            SessionId = sessionId,
            StationId = Guid.NewGuid(),
            StationName = "Cached Station",
            StationAddress = "123 Cached St",
            ConnectorNumber = 1,
            Status = SessionStatus.InProgress,
            StartTime = DateTime.UtcNow.AddMinutes(-30),
            EnergyKwh = 15.5m,
            CurrentCost = 54_250m,
            RatePerKwh = 3500m
        };

        var cacheKey = $"user:{userId}:active-session";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<ActiveSessionDto?>>>(), Arg.Any<TimeSpan?>())
            .Returns(cachedSession);

        // Act
        var result = await _service.GetActiveSessionAsync(userId);

        // Assert
        result.ShouldNotBeNull();
        result!.SessionId.ShouldBe(sessionId);
        result.StationName.ShouldBe("Cached Station");
        result.EnergyKwh.ShouldBe(15.5m);
        result.Status.ShouldBe(SessionStatus.InProgress);

        await _cache.Received(1).GetOrSetAsync(cacheKey, Arg.Any<Func<Task<ActiveSessionDto?>>>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetActiveSession_Should_Query_DB_On_Cache_Miss()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "ACT-001", "Active Station", "456 Active Ave", 21.0, 105.8);
            var connector = station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            connector.UpdateStatus(ConnectorStatus.Charging);
            await _dbContext.ChargingStations.AddAsync(station);

            var session = new ChargingSession(sessionId, userId, stationId, 1, ratePerKwh: 3500);
            session.MarkStarting();
            session.RecordStart(1001, 0);
            await _dbContext.ChargingSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        var cacheKey = $"user:{userId}:active-session";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<ActiveSessionDto?>>>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                var factory = callInfo.ArgAt<Func<Task<ActiveSessionDto?>>>(1);
                return factory();
            });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetActiveSessionAsync(userId);

            // Assert
            result.ShouldNotBeNull();
            result!.SessionId.ShouldBe(sessionId);
            result.StationName.ShouldBe("Active Station");
            result.Status.ShouldBe(SessionStatus.InProgress);
        });
    }

    [Fact]
    public async Task StartSession_Should_Invalidate_Cache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "INV-001", "Invalidate Station", "789 Inv St", 21.0, 105.8);
            var connector = station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            connector.UpdateStatus(ConnectorStatus.Available);
            await _dbContext.ChargingStations.AddAsync(station);

            // Seed AppUser with wallet balance for wallet check
            var appUser = new AppUser(Guid.NewGuid(), userId, "Test User", "0900000001");
            appUser.AddToWallet(100_000m);
            await _dbContext.AppUsers.AddAsync(appUser);

            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.StartSessionAsync(userId, new StartSessionRequest
            {
                StationId = stationId,
                ConnectorNumber = 1
            });

            // Assert - session created
            result.Success.ShouldBeTrue();
        });

        // Verify cache invalidation for station connectors and user active session
        await _cache.Received(1).RemoveAsync($"station:{stationId}:connectors");
        await _cache.Received(1).RemoveAsync($"user:{userId}:active-session");
    }

    [Fact]
    public async Task StopSession_Should_Invalidate_Cache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var session = new ChargingSession(sessionId, userId, Guid.NewGuid(), 1);
            session.MarkStarting();
            session.RecordStart(1, 0);
            await _dbContext.ChargingSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.StopSessionAsync(userId, sessionId);

            // Assert
            result.Success.ShouldBeTrue();
            result.Status.ShouldBe(SessionStatus.Stopping);
        });

        // Verify cache invalidation for user active session and session detail
        await _cache.Received(1).RemoveAsync($"user:{userId}:active-session");
        await _cache.Received(1).RemoveAsync($"session:{sessionId}:detail");
    }

    [Fact]
    public async Task GetSessionHistory_Should_Use_Cursor_Based_Pagination()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        var sessionIds = new List<Guid>();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "HIST-001", "History Station", "111 Hist St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);

            // Create 5 completed sessions
            for (int i = 0; i < 5; i++)
            {
                var sid = Guid.NewGuid();
                sessionIds.Add(sid);
                var session = new ChargingSession(sid, userId, stationId, 1, ratePerKwh: 3500);
                session.MarkStarting();
                session.RecordStart(1000 + i, 0);
                session.MarkStopping();
                session.RecordStop(10000 + (i * 1000));
                await _dbContext.ChargingSessions.AddAsync(session);
            }
            await _dbContext.SaveChangesAsync();
        });

        // Act - first page
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetSessionHistoryAsync(userId, null, 3);

            // Assert
            result.ShouldNotBeNull();
            result.Data.Count.ShouldBe(3);
            result.HasMore.ShouldBeTrue();
            result.NextCursor.ShouldNotBeNull();
            result.PageSize.ShouldBe(3);
        });

        // Session history does NOT use cache (direct DB query)
        await _cache.DidNotReceive().GetOrSetAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<PagedResult<SessionHistoryDto>>>>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetActiveSession_Should_Use_Correct_Cache_Key_Format()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedCacheKey = $"user:{userId}:active-session";

        _cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<Task<ActiveSessionDto?>>>(), Arg.Any<TimeSpan?>())
            .Returns((ActiveSessionDto?)null);

        // Act
        await _service.GetActiveSessionAsync(userId);

        // Assert - verify exact cache key format
        await _cache.Received(1).GetOrSetAsync(
            expectedCacheKey,
            Arg.Any<Func<Task<ActiveSessionDto?>>>(),
            Arg.Any<TimeSpan?>());
    }
}
