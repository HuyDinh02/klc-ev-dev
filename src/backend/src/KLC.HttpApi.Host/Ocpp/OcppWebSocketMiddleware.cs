using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using KLC.Stations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace KLC.Ocpp;

/// <summary>
/// ASP.NET Core middleware to handle OCPP WebSocket connections.
/// </summary>
public class OcppWebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OcppWebSocketMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private const int BufferSize = 4096;
    private static readonly Regex CpIdPattern = new(@"^[A-Za-z0-9\-_.]{1,64}$", RegexOptions.Compiled);

    public OcppWebSocketMiddleware(
        RequestDelegate next,
        ILogger<OcppWebSocketMiddleware> logger,
        IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(
        HttpContext context,
        OcppConnectionManager connectionManager)
    {
        // Check if this is an OCPP WebSocket request
        if (!context.Request.Path.StartsWithSegments("/ocpp"))
        {
            await _next(context);
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            _logger.LogWarning("Non-WebSocket request to OCPP endpoint from {RemoteIp}",
                context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket connection required");
            return;
        }

        // Extract ChargePoint ID from path: /ocpp/{chargePointId}
        var pathSegments = context.Request.Path.Value?.Split('/') ?? [];
        if (pathSegments.Length < 3 || string.IsNullOrEmpty(pathSegments[2]))
        {
            _logger.LogWarning("Missing ChargePoint ID in OCPP request");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("ChargePoint ID required in path");
            return;
        }

        var chargePointId = pathSegments[2];

        // Validate cpId format: [A-Za-z0-9-_.] max 64 chars
        if (!CpIdPattern.IsMatch(chargePointId))
        {
            _logger.LogWarning("Invalid ChargePoint ID format: {ChargePointId}", chargePointId);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid ChargePoint ID format");
            return;
        }

        // Authenticate via HTTP Basic Auth if station has OcppPassword set
        using (var authScope = _scopeFactory.CreateScope())
        {
            var uowManager = authScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            var stationRepo = authScope.ServiceProvider.GetRequiredService<IRepository<ChargingStation, Guid>>();

            using var uow = uowManager.Begin(requiresNew: true);
            var station = await stationRepo.FirstOrDefaultAsync(s => s.StationCode == chargePointId);

            if (station?.OcppPassword != null)
            {
                var authHeader = context.Request.Headers.Authorization.ToString();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("OCPP auth failed: missing Basic Auth header for {ChargePointId}", chargePointId);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"OCPP\"";
                    return;
                }

                try
                {
                    var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader[6..]));
                    var parts = credentials.Split(':', 2);
                    if (parts.Length != 2 || parts[1] != station.OcppPassword)
                    {
                        _logger.LogWarning("OCPP auth failed: invalid credentials for {ChargePointId}", chargePointId);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"OCPP\"";
                        return;
                    }
                }
                catch (FormatException)
                {
                    _logger.LogWarning("OCPP auth failed: malformed Basic Auth for {ChargePointId}", chargePointId);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }

            await uow.CompleteAsync();
        }

        _logger.LogInformation("WebSocket connection request from ChargePoint {ChargePointId}", chargePointId);

        // Accept WebSocket with OCPP subprotocol
        var webSocket = await context.WebSockets.AcceptWebSocketAsync("ocpp1.6");
        var connection = connectionManager.AddConnection(chargePointId, webSocket);

        try
        {
            await HandleWebSocketAsync(connection);
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "WebSocket error from {ChargePointId}", chargePointId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WebSocket from {ChargePointId}", chargePointId);
        }
        finally
        {
            connectionManager.RemoveConnection(chargePointId);

            // Clean up orphaned sessions for the disconnected station
            try
            {
                using var cleanupScope = _scopeFactory.CreateScope();
                var uowManager = cleanupScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var ocppService = cleanupScope.ServiceProvider.GetRequiredService<IOcppService>();

                using var uow = uowManager.Begin(requiresNew: true);
                await ocppService.HandleStationDisconnectAsync(chargePointId);
                await uow.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean up orphaned sessions for {ChargePointId}", chargePointId);
            }

            if (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection closed",
                        CancellationToken.None);
                }
                catch
                {
                    // Ignore close errors
                }
            }
        }
    }

    private async Task HandleWebSocketAsync(OcppConnection connection)
    {
        var buffer = new byte[BufferSize];
        var messageBuilder = new StringBuilder();

        while (connection.WebSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await connection.WebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("ChargePoint {ChargePointId} initiated close",
                        connection.ChargePointId);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                    if (result.EndOfMessage)
                    {
                        var message = messageBuilder.ToString();
                        messageBuilder.Clear();

                        // Create a fresh DI scope per message so scoped services
                        // (DbContext, repositories, etc.) get a fresh lifetime.
                        using var scope = _scopeFactory.CreateScope();
                        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                        var messageHandler = scope.ServiceProvider.GetRequiredService<OcppMessageHandler>();

                        // ABP repositories require an active Unit of Work.
                        using var uow = uowManager.Begin(requiresNew: true);
                        var response = await messageHandler.HandleMessageAsync(connection, message);
                        await uow.CompleteAsync();

                        if (!string.IsNullOrEmpty(response))
                        {
                            await SendMessageAsync(connection.WebSocket, response);
                        }
                    }
                }
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                _logger.LogWarning("ChargePoint {ChargePointId} disconnected unexpectedly",
                    connection.ChargePointId);
                break;
            }
        }
    }

    private async Task SendMessageAsync(WebSocket webSocket, string message)
    {
        if (webSocket.State != WebSocketState.Open)
            return;

        var bytes = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }
}
