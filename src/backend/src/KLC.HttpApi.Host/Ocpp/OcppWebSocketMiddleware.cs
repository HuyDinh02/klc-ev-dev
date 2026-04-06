using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Hubs;
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

    /// <summary>
    /// Supported OCPP subprotocols in order of preference (newest first).
    /// </summary>
    private static readonly string[] SupportedSubprotocols = ["ocpp2.0.1", "ocpp1.6"];

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

        // Resolve station and enforce admission before accepting the WebSocket.
        using (var authScope = _scopeFactory.CreateScope())
        {
            var uowManager = authScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            var stationRepo = authScope.ServiceProvider.GetRequiredService<IRepository<ChargingStation, Guid>>();

            using var uow = uowManager.Begin(requiresNew: true);
            var station = await stationRepo.FirstOrDefaultAsync(s => s.StationCode == chargePointId);

            if (station == null)
            {
                _logger.LogWarning("OCPP handshake rejected: unknown station {ChargePointId}", chargePointId);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("Unknown station");
                return;
            }

            if (!station.IsEnabled || station.Status == StationStatus.Disabled)
            {
                _logger.LogWarning(
                    "OCPP handshake rejected: station {ChargePointId} is not allowed to connect (enabled={IsEnabled}, status={Status})",
                    chargePointId,
                    station.IsEnabled,
                    station.Status);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Station is not allowed to connect");
                return;
            }

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
                    if (parts.Length != 2 || !string.Equals(parts[0], chargePointId, StringComparison.Ordinal) || parts[1] != station.OcppPassword)
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
                    context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"OCPP\"";
                    return;
                }
            }

            await uow.CompleteAsync();
        }

        _logger.LogInformation("WebSocket connection request from ChargePoint {ChargePointId}", chargePointId);

        // Negotiate OCPP subprotocol from client's Sec-WebSocket-Protocol header
        var (subprotocol, ocppVersion) = NegotiateSubprotocol(context);

        _logger.LogInformation("ChargePoint {ChargePointId} negotiated protocol: {Subprotocol} ({OcppVersion})",
            chargePointId, subprotocol, ocppVersion);

        // Accept WebSocket with negotiated OCPP subprotocol
        var webSocket = await context.WebSockets.AcceptWebSocketAsync(subprotocol);
        var connection = connectionManager.AddConnection(chargePointId, webSocket, ocppVersion);

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
            // Only run disconnect cleanup if this connection is still the active one.
            // A charger may reconnect before the old receive loop exits; in that case
            // RemoveConnection returns false and we skip station disconnect to avoid
            // stomping on the new connection.
            var wasActive = connectionManager.RemoveConnection(chargePointId, connection);

            if (wasActive)
            {
                // Clean up orphaned sessions for the disconnected station
                try
                {
                    using var cleanupScope = _scopeFactory.CreateScope();
                    var uowManager = cleanupScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                    var ocppService = cleanupScope.ServiceProvider.GetRequiredService<IOcppService>();
                    var notifier = cleanupScope.ServiceProvider.GetRequiredService<IMonitoringNotifier>();
                    var stationRepo = cleanupScope.ServiceProvider.GetRequiredService<IRepository<Stations.ChargingStation, Guid>>();

                    using var uow = uowManager.Begin(requiresNew: true);

                    // Capture status before disconnect processing
                    var station = await stationRepo.FirstOrDefaultAsync(s => s.StationCode == chargePointId);
                    var previousStatus = station?.Status;

                    await ocppService.HandleStationDisconnectAsync(chargePointId);
                    await uow.CompleteAsync();

                    // Broadcast station status change via SignalR
                    if (station != null && previousStatus.HasValue && previousStatus.Value != StationStatus.Offline)
                    {
                        await notifier.NotifyStationStatusChangedAsync(
                            station.Id,
                            station.Name,
                            previousStatus.Value,
                            StationStatus.Offline);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to clean up orphaned sessions for {ChargePointId}", chargePointId);
                }
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

                        // Wrap message processing in its own try-catch so that any exception
                        // from the handler or uow.CompleteAsync() (e.g. transient DB error)
                        // is logged and the receive loop continues rather than closing the WebSocket.
                        try
                        {
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
                        catch (WebSocketException)
                        {
                            throw; // Let WebSocket errors propagate to the outer handler
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Unhandled error processing OCPP message from {ChargePointId} — receive loop continues",
                                connection.ChargePointId);
                        }

                        // After sending BootNotification response, push configuration to the charger
                        if (connection.PendingPostBootConfig)
                        {
                            connection.PendingPostBootConfig = false;
                            var scopeFactory = _scopeFactory;
                            var conn = connection;
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    // Brief delay to let the charger process BootNotification.conf
                                    await Task.Delay(500);

                                    using var configScope = scopeFactory.CreateScope();
                                    var uowManager = configScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                                    var configService = configScope.ServiceProvider.GetRequiredService<OcppPostBootConfigService>();

                                    using var uow = uowManager.Begin(requiresNew: true);
                                    await configService.SendPostBootConfigurationAsync(conn);
                                    await uow.CompleteAsync();
                                }
                                catch (Exception ex)
                                {
                                    // Log but don't crash — post-boot config is best-effort
                                    var logger = scopeFactory.CreateScope().ServiceProvider
                                        .GetRequiredService<ILogger<OcppWebSocketMiddleware>>();
                                    logger.LogWarning(ex,
                                        "Failed to send post-boot configuration to {ChargePointId}",
                                        conn.ChargePointId);
                                }
                            });
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

    /// <summary>
    /// Negotiate OCPP subprotocol from the client's Sec-WebSocket-Protocol header.
    /// Returns the best matching subprotocol and its version enum.
    /// Falls back to ocpp1.6 if no match or no header.
    /// </summary>
    private static (string subprotocol, OcppProtocolVersion version) NegotiateSubprotocol(HttpContext context)
    {
        var requestedProtocols = context.WebSockets.WebSocketRequestedProtocols;

        if (requestedProtocols.Count > 0)
        {
            // Pick the first supported subprotocol that the client also supports
            foreach (var supported in SupportedSubprotocols)
            {
                if (requestedProtocols.Contains(supported, StringComparer.OrdinalIgnoreCase))
                {
                    return supported switch
                    {
                        "ocpp2.0.1" => (supported, OcppProtocolVersion.Ocpp201),
                        _ => (supported, OcppProtocolVersion.Ocpp16J)
                    };
                }
            }
        }

        // Default: OCPP 1.6J (most chargers in the field)
        return ("ocpp1.6", OcppProtocolVersion.Ocpp16J);
    }
}
