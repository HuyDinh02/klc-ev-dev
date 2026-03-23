using System.Net;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace KLC.Ocpp;

/// <summary>
/// ASP.NET Core middleware for handling OCPP WebSocket connections.
/// Accepts connections at /ocpp/{chargePointId} with OCPP 1.6J sub-protocol.
/// </summary>
public class OcppWebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OcppWebSocketMiddleware> _logger;
    private readonly string _ocppPath;
    private const int BufferSize = 65536; // 64KB buffer
    private const int MaxMessageSize = 1048576; // 1MB max message

    public OcppWebSocketMiddleware(RequestDelegate next, ILogger<OcppWebSocketMiddleware> logger, string ocppPath = "/ocpp")
    {
        _next = next;
        _logger = logger;
        _ocppPath = ocppPath;
    }

    public async Task InvokeAsync(HttpContext context, OcppConnectionManager connectionManager, OcppMessageRouter router)
    {
        if (!context.Request.Path.StartsWithSegments(_ocppPath))
        {
            await _next(context);
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Not a WebSocket request");
            return;
        }

        var chargePointId = ExtractChargePointId(context.Request.Path.Value);
        if (string.IsNullOrEmpty(chargePointId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid charge point ID in URL");
            return;
        }

        var requestedSubProtocols = context.Request.Headers.GetCommaSeparatedValues("Sec-WebSocket-Protocol");
        var hasOcppSubProtocol = requestedSubProtocols.Any(p => p.Equals("ocpp1.6", StringComparison.OrdinalIgnoreCase));

        if (!hasOcppSubProtocol)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Required sub-protocol 'ocpp1.6' not requested");
            return;
        }

        WebSocketAcceptContext acceptContext = new()
        {
            SubProtocol = "ocpp1.6"
        };

        var webSocket = await context.WebSockets.AcceptWebSocketAsync(acceptContext);

        var connection = new OcppConnection
        {
            ChargePointId = chargePointId,
            WebSocket = webSocket,
            ConnectedAt = DateTime.UtcNow,
            LastHeartbeat = DateTime.UtcNow,
            LastMessageReceived = DateTime.UtcNow,
            Cts = new CancellationTokenSource()
        };

        if (!connectionManager.TryAdd(chargePointId, connection))
        {
            _logger.LogWarning("Charge point {ChargePointId} attempted to connect but connection already exists", chargePointId);
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection already exists", CancellationToken.None);
            await webSocket.DisposeAsync();
            return;
        }

        _logger.LogInformation("Charge point {ChargePointId} connected", chargePointId);

        try
        {
            await HandleConnectionAsync(connection, router, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception handling connection for charge point {ChargePointId}", chargePointId);
        }
        finally
        {
            if (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing connection",
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception closing WebSocket for {ChargePointId}", chargePointId);
                }
            }

            webSocket.Dispose();
            connectionManager.TryRemove(chargePointId, out _);
            connection.Cts.Dispose();
            _logger.LogInformation("Charge point {ChargePointId} disconnected", chargePointId);
        }
    }

    private async Task HandleConnectionAsync(OcppConnection connection, OcppMessageRouter router, CancellationToken externalCt)
    {
        var buffer = new byte[BufferSize];
        var messageBuffer = new MemoryStream();

        try
        {
            while (connection.WebSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await connection.WebSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        connection.Cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Connection for {ChargePointId} cancelled", connection.ChargePointId);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("Received close frame from {ChargePointId}", connection.ChargePointId);
                    break;
                }

                if (result.MessageType != WebSocketMessageType.Text)
                {
                    _logger.LogWarning("Received non-text message from {ChargePointId}", connection.ChargePointId);
                    continue;
                }

                messageBuffer.Write(buffer, 0, result.Count);

                if (messageBuffer.Length > MaxMessageSize)
                {
                    _logger.LogWarning(
                        "Message from {ChargePointId} exceeds maximum size ({Size} > {MaxSize})",
                        connection.ChargePointId, messageBuffer.Length, MaxMessageSize);
                    messageBuffer.SetLength(0);
                    continue;
                }

                if (!result.EndOfMessage)
                {
                    continue;
                }

                connection.LastMessageReceived = DateTime.UtcNow;

                var messageText = System.Text.Encoding.UTF8.GetString(messageBuffer.ToArray());
                messageBuffer.SetLength(0);

                JArray? message;
                try
                {
                    message = JArray.Parse(messageText);
                }
                catch (Newtonsoft.Json.JsonException ex)
                {
                    _logger.LogWarning(ex, "Received malformed JSON from {ChargePointId}", connection.ChargePointId);
                    continue;
                }

                if (message == null || message.Count == 0)
                {
                    _logger.LogWarning("Received empty OCPP message from {ChargePointId}", connection.ChargePointId);
                    continue;
                }

                var responseMessage = await router.RouteAsync(
                    connection.ChargePointId,
                    message,
                    connection,
                    connection.Cts.Token);

                if (responseMessage != null)
                {
                    try
                    {
                        var responseData = System.Text.Encoding.UTF8.GetBytes(
                            responseMessage.ToString(Newtonsoft.Json.Formatting.None));

                        await connection.WebSocket.SendAsync(
                            new ArraySegment<byte>(responseData),
                            WebSocketMessageType.Text,
                            true,
                            connection.Cts.Token);

                        _logger.LogDebug(
                            "Sent response to {ChargePointId}: {Response}",
                            connection.ChargePointId,
                            responseMessage);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Exception sending response to {ChargePointId}", connection.ChargePointId);
                        break;
                    }
                }
            }
        }
        finally
        {
            messageBuffer.Dispose();
        }
    }

    private static string? ExtractChargePointId(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        var match = Regex.Match(path, @"/ocpp/([^/?]+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var id = match.Groups[1].Value;
            // Validate charge point ID format: KLC-{CITY}-{SEQ}
            if (Regex.IsMatch(id, @"^[a-zA-Z0-9\-]{1,50}$"))
            {
                return id;
            }
        }

        return null;
    }
}
