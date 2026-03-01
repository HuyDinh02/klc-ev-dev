using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace KCharge.Ocpp;

/// <summary>
/// ASP.NET Core middleware to handle OCPP WebSocket connections.
/// </summary>
public class OcppWebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OcppWebSocketMiddleware> _logger;
    private const int BufferSize = 4096;

    public OcppWebSocketMiddleware(RequestDelegate next, ILogger<OcppWebSocketMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        OcppConnectionManager connectionManager,
        OcppMessageHandler messageHandler)
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
        _logger.LogInformation("WebSocket connection request from ChargePoint {ChargePointId}", chargePointId);

        // Accept WebSocket with OCPP subprotocol
        var webSocket = await context.WebSockets.AcceptWebSocketAsync("ocpp1.6");
        var connection = connectionManager.AddConnection(chargePointId, webSocket);

        try
        {
            await HandleWebSocketAsync(connection, messageHandler);
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

    private async Task HandleWebSocketAsync(OcppConnection connection, OcppMessageHandler messageHandler)
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

                        var response = await messageHandler.HandleMessageAsync(connection, message);

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
