using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace KLC.Ocpp;

/// <summary>
/// Redis pub/sub bridge for cross-instance OCPP commands.
///
/// Problem: After Cloud Run deploys a new revision, the charger's WebSocket stays
/// on the old instance. HTTP-based command forwarding can't reach it.
///
/// Solution: Publish OCPP commands to a Redis channel. ALL Gateway instances
/// subscribe. The instance holding the charger's WebSocket picks it up, executes
/// the command, and publishes the result back on a response channel.
///
/// Flow:
///   BFF/Admin API → OcppRemoteCommandService.SendCommandAsync
///     → local connection found? → execute directly
///     → not found? → publish to Redis "ocpp:cmd:{stationCode}"
///     → Gateway instance with WebSocket subscribes, executes, publishes result
///     → Original caller receives result via Redis "ocpp:result:{requestId}"
/// </summary>
public class OcppRedisCommandBridge : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OcppConnectionManager _connectionManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OcppRedisCommandBridge> _logger;
    private ISubscriber? _subscriber;
    private IConnectionMultiplexer? _redis;

    public OcppRedisCommandBridge(
        IServiceProvider serviceProvider,
        OcppConnectionManager connectionManager,
        IConfiguration configuration,
        ILogger<OcppRedisCommandBridge> logger)
    {
        _serviceProvider = serviceProvider;
        _connectionManager = connectionManager;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var redisConn = _configuration["ConnectionStrings:Redis"];
        if (string.IsNullOrEmpty(redisConn))
        {
            _logger.LogWarning("Redis not configured — OcppRedisCommandBridge disabled");
            return;
        }

        try
        {
            _redis = await ConnectionMultiplexer.ConnectAsync(redisConn);
            _subscriber = _redis.GetSubscriber();

            // Subscribe to OCPP command requests
            await _subscriber.SubscribeAsync(
                RedisChannel.Literal("ocpp:cmd"),
                async (channel, message) => await HandleCommandAsync(message!));

            _logger.LogInformation("OcppRedisCommandBridge started — listening on ocpp:cmd channel");

            // Keep alive until shutdown
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OcppRedisCommandBridge failed");
        }
    }

    private async Task HandleCommandAsync(string message)
    {
        try
        {
            var cmd = JsonSerializer.Deserialize<OcppRedisCommand>(message);
            if (cmd == null) return;

            // Only handle if the charger is connected to THIS instance
            var connection = _connectionManager.GetConnection(cmd.StationCode);
            if (connection == null)
            {
                // Not on this instance — another instance will handle it
                return;
            }

            _logger.LogInformation(
                "REDIS_CMD: Handling {Action} for {StationCode} [requestId={RequestId}]",
                cmd.Action, cmd.StationCode, cmd.RequestId);

            string? response;
            try
            {
                response = await connection.SendCallAsync(cmd.Action, cmd.Payload, TimeSpan.FromSeconds(30));
            }
            catch (Exception ex)
            {
                response = $"ERROR:{ex.Message}";
            }

            // Publish result back
            var result = new OcppRedisResult
            {
                RequestId = cmd.RequestId,
                Response = response
            };

            await _subscriber!.PublishAsync(
                RedisChannel.Literal($"ocpp:result:{cmd.RequestId}"),
                JsonSerializer.Serialize(result));

            _logger.LogInformation(
                "REDIS_CMD_RESULT: {Action} for {StationCode} completed [requestId={RequestId}]",
                cmd.Action, cmd.StationCode, cmd.RequestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Redis OCPP command");
        }
    }

    /// <summary>
    /// Send a command via Redis pub/sub and wait for the result.
    /// Called by OcppRemoteCommandService when the station isn't connected locally.
    /// </summary>
    public async Task<string?> SendCommandViaRedisAsync(string stationCode, string action, object payload)
    {
        if (_subscriber == null || _redis == null)
            return null;

        var requestId = Guid.NewGuid().ToString("N")[..12];
        var cmd = new OcppRedisCommand
        {
            RequestId = requestId,
            StationCode = stationCode,
            Action = action,
            Payload = payload
        };

        // Subscribe to result channel before publishing
        var tcs = new TaskCompletionSource<string?>();
        var resultChannel = RedisChannel.Literal($"ocpp:result:{requestId}");

        await _subscriber.SubscribeAsync(resultChannel, (_, msg) =>
        {
            try
            {
                var result = JsonSerializer.Deserialize<OcppRedisResult>(msg.ToString());
                tcs.TrySetResult(result?.Response);
            }
            catch
            {
                tcs.TrySetResult(null);
            }
        });

        // Publish command
        await _subscriber.PublishAsync(RedisChannel.Literal("ocpp:cmd"), JsonSerializer.Serialize(cmd));

        // Wait for result with timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(35));
        cts.Token.Register(() => tcs.TrySetResult(null));

        var response = await tcs.Task;

        // Cleanup subscription
        await _subscriber.UnsubscribeAsync(resultChannel);

        return response;
    }

    public bool IsAvailable => _subscriber != null && _redis is { IsConnected: true };
}

public class OcppRedisCommand
{
    public string RequestId { get; set; } = "";
    public string StationCode { get; set; } = "";
    public string Action { get; set; } = "";
    public object? Payload { get; set; }
}

public class OcppRedisResult
{
    public string RequestId { get; set; } = "";
    public string? Response { get; set; }
}
