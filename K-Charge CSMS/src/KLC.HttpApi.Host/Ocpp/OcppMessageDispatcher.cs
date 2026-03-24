using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KLC.Ocpp;

/// <summary>
/// Sends OCPP CALL commands to Charge Points and waits for responses.
/// Implements IOcppMessageDispatcher from Domain layer.
/// </summary>
public class OcppMessageDispatcher : IOcppMessageDispatcher
{
    private readonly OcppConnectionManager _connectionManager;
    private readonly ILogger<OcppMessageDispatcher> _logger;
    private const int DefaultTimeoutSeconds = 30;

    public OcppMessageDispatcher(
        OcppConnectionManager connectionManager,
        ILogger<OcppMessageDispatcher> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task<JObject> SendCommandAsync(
        string chargePointId,
        string action,
        JObject payload,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var connection = _connectionManager.GetConnection(chargePointId)
            ?? throw new ChargePointNotConnectedException(chargePointId);

        var uniqueId = Guid.NewGuid().ToString("N")[..36];
        var callMessage = new JArray(2, uniqueId, action, payload);
        var tcs = new TaskCompletionSource<JArray>();

        if (!connection.PendingRequests.TryAdd(uniqueId, tcs))
        {
            throw new InvalidOperationException(
                $"Failed to register pending request {uniqueId} for {chargePointId}");
        }

        try
        {
            var timeoutDuration = timeout ?? TimeSpan.FromSeconds(DefaultTimeoutSeconds);
            var sendData = System.Text.Encoding.UTF8.GetBytes(
                callMessage.ToString(Formatting.None));

            _logger.LogDebug(
                "Sending OCPP command to {ChargePointId}: {Action} (ID: {UniqueId})",
                chargePointId, action, uniqueId);

            await connection.WebSocket.SendAsync(
                new ArraySegment<byte>(sendData),
                WebSocketMessageType.Text,
                true,
                cancellationToken);

            // Wait for response with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutDuration);

            try
            {
                var responseArray = await tcs.Task.WaitAsync(cts.Token);

                _logger.LogDebug(
                    "Received response for {ChargePointId} command {Action} (ID: {UniqueId})",
                    chargePointId, action, uniqueId);

                // responseArray is the full CALLRESULT [3, uniqueId, payload]
                // or it might be just the payload depending on router handling
                // Extract the JObject payload
                if (responseArray.Count > 0 && responseArray[0] is JObject resultPayload)
                {
                    return resultPayload;
                }

                // If the router set result as the full message content
                return responseArray.Count > 0
                    ? responseArray[0] as JObject ?? new JObject()
                    : new JObject();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Command timeout for {ChargePointId}, action {Action}, timeout {Timeout}s",
                    chargePointId, action, timeoutDuration.TotalSeconds);
                throw new OcppCommandTimeoutException(chargePointId, action, timeoutDuration);
            }
        }
        finally
        {
            connection.PendingRequests.TryRemove(uniqueId, out _);
        }
    }

    public bool IsConnected(string chargePointId)
    {
        return _connectionManager.IsConnected(chargePointId);
    }

    public IEnumerable<string> GetConnectedChargePoints()
    {
        return _connectionManager.GetConnectedChargePoints();
    }
}
