using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Ocpp;
using KLC.Stations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace KLC.EntityFrameworkCore.Ocpp;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class OcppWebSocketMiddlewareTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;

    public OcppWebSocketMiddlewareTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
    }

    [Fact]
    public async Task InvokeAsync_Should_Return_404_For_Unknown_Station()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext("/ocpp/UNKNOWN-CP");

        await middleware.InvokeAsync(context, new OcppConnectionManager(NullLogger<OcppConnectionManager>.Instance));

        context.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task InvokeAsync_Should_Return_403_For_Disabled_Station()
    {
        await SeedStationAsync(station =>
        {
            station.Disable();
        }, "CP-DISABLED");

        var middleware = CreateMiddleware();
        var context = CreateContext("/ocpp/CP-DISABLED");

        await middleware.InvokeAsync(context, new OcppConnectionManager(NullLogger<OcppConnectionManager>.Instance));

        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task InvokeAsync_Should_Return_403_For_Decommissioned_Station()
    {
        await SeedStationAsync(station =>
        {
            station.Decommission();
        }, "CP-DECOM");

        var middleware = CreateMiddleware();
        var context = CreateContext("/ocpp/CP-DECOM");

        await middleware.InvokeAsync(context, new OcppConnectionManager(NullLogger<OcppConnectionManager>.Instance));

        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task InvokeAsync_Should_Return_401_For_BasicAuth_Username_Mismatch()
    {
        await SeedStationAsync(station =>
        {
            station.SetOcppPassword("secret");
        }, "CP-AUTH-001");

        var middleware = CreateMiddleware();
        var context = CreateContext("/ocpp/CP-AUTH-001");
        context.Request.Headers.Authorization = CreateBasicAuth("WRONG", "secret");

        await middleware.InvokeAsync(context, new OcppConnectionManager(NullLogger<OcppConnectionManager>.Instance));

        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_Should_Accept_WebSocket_For_Valid_Authenticated_Station()
    {
        await SeedStationAsync(station =>
        {
            station.SetOcppPassword("secret");
        }, "CP-AUTH-OK");

        var middleware = CreateMiddleware();
        var feature = new TestWebSocketFeature();
        var context = CreateContext("/ocpp/CP-AUTH-OK", feature);
        context.Request.Headers.Authorization = CreateBasicAuth("CP-AUTH-OK", "secret");
        context.Request.Headers["Sec-WebSocket-Protocol"] = "ocpp1.6";

        await middleware.InvokeAsync(context, new OcppConnectionManager(NullLogger<OcppConnectionManager>.Instance));

        feature.AcceptCalled.ShouldBeTrue();
        feature.AcceptedSubProtocol.ShouldBe("ocpp1.6");
    }

    private OcppWebSocketMiddleware CreateMiddleware()
    {
        return new OcppWebSocketMiddleware(
            _ => Task.CompletedTask,
            NullLogger<OcppWebSocketMiddleware>.Instance,
            GetRequiredService<IServiceScopeFactory>());
    }

    private async Task SeedStationAsync(Action<ChargingStation>? configure, string stationCode)
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(Guid.NewGuid(), stationCode, "Middleware Test", "123 OCPP St", 21.0, 105.8);
            configure?.Invoke(station);

            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });
    }

    private static DefaultHttpContext CreateContext(string path, TestWebSocketFeature? feature = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        context.Features.Set<IHttpWebSocketFeature>(feature ?? new TestWebSocketFeature());
        return context;
    }

    private static string CreateBasicAuth(string username, string password)
    {
        return $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"))}";
    }

    private sealed class TestWebSocketFeature : IHttpWebSocketFeature
    {
        private readonly WebSocket _webSocket = new ClosingTestWebSocket();

        public bool IsWebSocketRequest => true;

        public bool AcceptCalled { get; private set; }

        public string? AcceptedSubProtocol { get; private set; }

        public Task<WebSocket> AcceptAsync(WebSocketAcceptContext context)
        {
            AcceptCalled = true;
            AcceptedSubProtocol = context.SubProtocol;
            return Task.FromResult(_webSocket);
        }
    }

    private sealed class ClosingTestWebSocket : WebSocket
    {
        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override WebSocketState State => WebSocketState.Open;
        public override string SubProtocol => "ocpp1.6";

        public override void Abort()
        {
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            return Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
