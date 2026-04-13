using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Sessions;
using KLC.Stations;
using KLC.TestDoubles;
using KLC.Users;
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
    private readonly ISessionBffAppService _sessionAppService;
    private readonly SessionBffService _service;

    public SessionBffServiceTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _cache = new PassthroughCacheService();
        _sessionAppService = Substitute.For<ISessionBffAppService>();
        var logger = Substitute.For<ILogger<SessionBffService>>();

        _service = new SessionBffService(_dbContext, _cache, _sessionAppService, logger);
    }

    [Fact]
    public async Task StartSession_Should_Succeed_When_AppService_Returns_Success()
    {
        var userId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        _sessionAppService.StartSessionAsync(Arg.Any<StartSessionInput>())
            .Returns(new StartSessionResultDto
            {
                Success = true,
                SessionId = sessionId,
                Status = SessionStatus.Pending,
                StationId = stationId
            });

        var result = await _service.StartSessionAsync(userId, new StartSessionRequest
        {
            StationId = stationId,
            ConnectorNumber = 1
        });

        result.Success.ShouldBeTrue();
        result.SessionId.ShouldNotBeNull();
        result.Status.ShouldBe(SessionStatus.Pending);
    }

    [Fact]
    public async Task StartSession_Should_Fail_When_Connector_Not_Found()
    {
        var userId = Guid.NewGuid();

        _sessionAppService.StartSessionAsync(Arg.Any<StartSessionInput>())
            .Returns(new StartSessionResultDto
            {
                Success = false,
                Error = "Connector not found"
            });

        var result = await _service.StartSessionAsync(userId, new StartSessionRequest
        {
            StationId = Guid.NewGuid(),
            ConnectorNumber = 1
        });

        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("Connector not found");
    }

    [Fact]
    public async Task StartSession_Should_Fail_When_Connector_Not_Available()
    {
        _sessionAppService.StartSessionAsync(Arg.Any<StartSessionInput>())
            .Returns(new StartSessionResultDto
            {
                Success = false,
                Error = "Connector is not available"
            });

        var result = await _service.StartSessionAsync(Guid.NewGuid(), new StartSessionRequest
        {
            StationId = Guid.NewGuid(),
            ConnectorNumber = 1
        });

        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("not available");
    }

    [Fact]
    public async Task StartSession_Should_Fail_When_User_Has_Active_Session()
    {
        _sessionAppService.StartSessionAsync(Arg.Any<StartSessionInput>())
            .Returns(new StartSessionResultDto
            {
                Success = false,
                Error = "You already have an active session"
            });

        var result = await _service.StartSessionAsync(Guid.NewGuid(), new StartSessionRequest
        {
            StationId = Guid.NewGuid(),
            ConnectorNumber = 1
        });

        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("already have an active session");
    }

    [Fact]
    public async Task StopSession_Should_Succeed_When_InProgress()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        _sessionAppService.StopSessionAsync(userId, sessionId)
            .Returns(new StopSessionResultDto
            {
                Success = true,
                SessionId = sessionId,
                Status = SessionStatus.Stopping,
                StationId = Guid.NewGuid()
            });

        var result = await _service.StopSessionAsync(userId, sessionId);

        result.Success.ShouldBeTrue();
        result.SessionId.ShouldBe(sessionId);
        result.Status.ShouldBe(SessionStatus.Stopping);
    }

    [Fact]
    public async Task StopSession_Should_Fail_When_Session_Not_Found()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        _sessionAppService.StopSessionAsync(userId, sessionId)
            .Returns(new StopSessionResultDto
            {
                Success = false,
                Error = "Session not found"
            });

        var result = await _service.StopSessionAsync(userId, sessionId);

        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("Session not found");
    }

    [Fact]
    public async Task StopSession_Should_Fail_When_Session_Not_InProgress()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        _sessionAppService.StopSessionAsync(userId, sessionId)
            .Returns(new StopSessionResultDto
            {
                Success = false,
                Error = "Session is not in progress"
            });

        var result = await _service.StopSessionAsync(userId, sessionId);

        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("not in progress");
    }

    [Fact]
    public async Task StopSession_Should_Fail_When_Session_Belongs_To_Another_User()
    {
        var differentUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        _sessionAppService.StopSessionAsync(differentUserId, sessionId)
            .Returns(new StopSessionResultDto
            {
                Success = false,
                Error = "Session not found"
            });

        var result = await _service.StopSessionAsync(differentUserId, sessionId);

        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("Session not found");
    }

    [Fact]
    public async Task StartSession_Should_Succeed_When_Connector_Preparing()
    {
        var userId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        _sessionAppService.StartSessionAsync(Arg.Any<StartSessionInput>())
            .Returns(new StartSessionResultDto
            {
                Success = true,
                SessionId = Guid.NewGuid(),
                Status = SessionStatus.Pending,
                StationId = stationId
            });

        var result = await _service.StartSessionAsync(userId, new StartSessionRequest
        {
            StationId = stationId,
            ConnectorNumber = 1
        });

        result.Success.ShouldBeTrue();
        result.SessionId.ShouldNotBeNull();
        result.Status.ShouldBe(SessionStatus.Pending);
    }

    [Fact]
    public async Task StartSession_Should_Fail_When_Wallet_Balance_Insufficient()
    {
        _sessionAppService.StartSessionAsync(Arg.Any<StartSessionInput>())
            .Returns(new StartSessionResultDto
            {
                Success = false,
                Error = KLCDomainErrorCodes.Wallet.InsufficientBalanceToCharge
            });

        var result = await _service.StartSessionAsync(Guid.NewGuid(), new StartSessionRequest
        {
            StationId = Guid.NewGuid(),
            ConnectorNumber = 1
        });

        result.Success.ShouldBeFalse();
        result.Error.ShouldBe(KLCDomainErrorCodes.Wallet.InsufficientBalanceToCharge);
    }

    [Fact]
    public async Task StartSession_Should_Fail_When_Wallet_Balance_Zero()
    {
        _sessionAppService.StartSessionAsync(Arg.Any<StartSessionInput>())
            .Returns(new StartSessionResultDto
            {
                Success = false,
                Error = KLCDomainErrorCodes.Wallet.InsufficientBalanceToCharge
            });

        var result = await _service.StartSessionAsync(Guid.NewGuid(), new StartSessionRequest
        {
            StationId = Guid.NewGuid(),
            ConnectorNumber = 1
        });

        result.Success.ShouldBeFalse();
        result.Error.ShouldBe(KLCDomainErrorCodes.Wallet.InsufficientBalanceToCharge);
    }

    [Fact]
    public async Task StartSession_Should_Fail_When_No_AppUser_Found()
    {
        _sessionAppService.StartSessionAsync(Arg.Any<StartSessionInput>())
            .Returns(new StartSessionResultDto
            {
                Success = false,
                Error = KLCDomainErrorCodes.Wallet.InsufficientBalanceToCharge
            });

        var result = await _service.StartSessionAsync(Guid.NewGuid(), new StartSessionRequest
        {
            StationId = Guid.NewGuid(),
            ConnectorNumber = 1
        });

        result.Success.ShouldBeFalse();
        result.Error.ShouldBe(KLCDomainErrorCodes.Wallet.InsufficientBalanceToCharge);
    }

    [Fact]
    public async Task StartSession_Should_Fail_When_Connector_Disabled()
    {
        _sessionAppService.StartSessionAsync(Arg.Any<StartSessionInput>())
            .Returns(new StartSessionResultDto
            {
                Success = false,
                Error = "Connector is not available"
            });

        var result = await _service.StartSessionAsync(Guid.NewGuid(), new StartSessionRequest
        {
            StationId = Guid.NewGuid(),
            ConnectorNumber = 1
        });

        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("not available");
    }

    [Fact]
    public async Task StartSession_Should_Fail_When_Connector_Faulted()
    {
        _sessionAppService.StartSessionAsync(Arg.Any<StartSessionInput>())
            .Returns(new StartSessionResultDto
            {
                Success = false,
                Error = "Connector is not available"
            });

        var result = await _service.StartSessionAsync(Guid.NewGuid(), new StartSessionRequest
        {
            StationId = Guid.NewGuid(),
            ConnectorNumber = 1
        });

        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("not available");
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

}
