using System.Net;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Fleets;
using KLC.Sessions;
using KLC.Stations;
using KLC.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace KLC.BffServices;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class SessionBffServiceTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly SessionBffService _service;

    public SessionBffServiceTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _cache = new PassthroughCacheService();
        var fleetPolicyService = Substitute.For<IFleetChargingPolicyService>();
        fleetPolicyService.ValidateChargingAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new FleetChargingValidationResult(true));
        var logger = Substitute.For<ILogger<SessionBffService>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Wallet:MinBalanceToStart"] = "10000" })
            .Build();

        // Mock HttpClientFactory to return a successful RemoteStart response
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var mockHandler = new MockHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true,\"message\":\"RemoteStartTransaction accepted\"}")
            });
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(mockHandler));

        _service = new SessionBffService(_dbContext, _cache, fleetPolicyService, configuration, httpClientFactory, logger);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public MockHttpMessageHandler(HttpResponseMessage response) => _response = response;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(_response);
    }

    [Fact]
    public async Task StartSession_Should_Succeed_When_Connector_Available()
    {
        var userId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        var connectorId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "KC-TEST-001", "Test Station", "123 Test St", 21.0, 105.8);
            var connector = station.AddConnector(connectorId, 1, ConnectorType.CCS2, 50);
            connector.UpdateStatus(ConnectorStatus.Available);
            await _dbContext.ChargingStations.AddAsync(station);

            // Seed AppUser with wallet balance for wallet check
            var appUser = new AppUser(Guid.NewGuid(), userId, "Test User", "0900000002");
            appUser.AddToWallet(100_000m);
            await _dbContext.AppUsers.AddAsync(appUser);

            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.StartSessionAsync(userId, new StartSessionRequest
            {
                StationId = stationId,
                ConnectorNumber = 1
            });

            result.Success.ShouldBeTrue();
            result.SessionId.ShouldNotBeNull();
            result.Status.ShouldBe(SessionStatus.Pending);
        });
    }

    [Fact]
    public async Task StartSession_Should_Fail_When_Connector_Not_Found()
    {
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.StartSessionAsync(userId, new StartSessionRequest
            {
                StationId = Guid.NewGuid(),
                ConnectorNumber = 1
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("Connector not found");
        });
    }

    [Fact]
    public async Task StartSession_Should_Fail_When_Connector_Not_Available()
    {
        var stationId = Guid.NewGuid();
        var connectorId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "KC-TEST-002", "Test Station 2", "456 Test St", 21.0, 105.8);
            var connector = station.AddConnector(connectorId, 1, ConnectorType.CCS2, 50);
            connector.UpdateStatus(ConnectorStatus.Charging); // Not available
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.StartSessionAsync(Guid.NewGuid(), new StartSessionRequest
            {
                StationId = stationId,
                ConnectorNumber = 1
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("not available");
        });
    }

    [Fact]
    public async Task StartSession_Should_Fail_When_User_Has_Active_Session()
    {
        var userId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "KC-TEST-003", "Test Station 3", "789 Test St", 21.0, 105.8);
            var connector = station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            connector.UpdateStatus(ConnectorStatus.Available);
            await _dbContext.ChargingStations.AddAsync(station);

            // Create an existing active session for the user
            var existingSession = new ChargingSession(Guid.NewGuid(), userId, stationId, 1);
            await _dbContext.ChargingSessions.AddAsync(existingSession);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.StartSessionAsync(userId, new StartSessionRequest
            {
                StationId = stationId,
                ConnectorNumber = 1
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("already have an active session");
        });
    }

    [Fact]
    public async Task StopSession_Should_Succeed_When_InProgress()
    {
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

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.StopSessionAsync(userId, sessionId);

            result.Success.ShouldBeTrue();
            result.SessionId.ShouldBe(sessionId);
            result.Status.ShouldBe(SessionStatus.Stopping);
        });
    }

    [Fact]
    public async Task StopSession_Should_Fail_When_Session_Not_Found()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.StopSessionAsync(Guid.NewGuid(), Guid.NewGuid());

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("Session not found");
        });
    }

    [Fact]
    public async Task StopSession_Should_Fail_When_Session_Not_InProgress()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            // Session is Pending, not InProgress
            var session = new ChargingSession(sessionId, userId, Guid.NewGuid(), 1);
            await _dbContext.ChargingSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.StopSessionAsync(userId, sessionId);

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("not in progress");
        });
    }

    [Fact]
    public async Task StopSession_Should_Fail_When_Session_Belongs_To_Another_User()
    {
        var sessionId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var session = new ChargingSession(sessionId, ownerId, Guid.NewGuid(), 1);
            session.MarkStarting();
            session.RecordStart(1, 0);
            await _dbContext.ChargingSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var differentUserId = Guid.NewGuid();
            var result = await _service.StopSessionAsync(differentUserId, sessionId);

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("Session not found");
        });
    }

    [Fact]
    public async Task GetActiveSession_Should_Return_Null_When_No_Active_Session()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetActiveSessionAsync(Guid.NewGuid());
            result.ShouldBeNull();
        });
    }

    [Fact]
    public async Task GetSessionHistory_Should_Return_Empty_When_No_Sessions()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetSessionHistoryAsync(Guid.NewGuid(), null, 20);

            result.ShouldNotBeNull();
            result.Data.ShouldBeEmpty();
            result.HasMore.ShouldBeFalse();
        });
    }

    /// <summary>
    /// Simple ICacheService that passes through to the factory (no caching in tests).
    /// </summary>
    private class PassthroughCacheService : ICacheService
    {
        public Task<T?> GetAsync<T>(string key) => Task.FromResult<T?>(default);
        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) => Task.CompletedTask;
        public Task RemoveAsync(string key) => Task.CompletedTask;
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
            => await factory();
    }
}
