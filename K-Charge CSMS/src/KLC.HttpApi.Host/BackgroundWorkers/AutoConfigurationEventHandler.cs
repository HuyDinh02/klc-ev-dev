using KLC.Ocpp;
using KLC.Ocpp.Events;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace KLC.BackgroundWorkers;

/// <summary>
/// Automatically configures standard OCPP settings when a charge point connects.
/// Triggered by ChargePointConnectedEto after successful BootNotification.
/// </summary>
public class AutoConfigurationEventHandler
    : IDistributedEventHandler<ChargePointConnectedEto>, ITransientDependency
{
    private readonly IOcppMessageDispatcher _dispatcher;
    private readonly ILogger<AutoConfigurationEventHandler> _logger;

    private static readonly Dictionary<string, string> StandardConfig = new()
    {
        ["HeartbeatInterval"] = "300",
        ["MeterValueSampleInterval"] = "60",
        ["ClockAlignedDataInterval"] = "900",
        ["MeterValuesSampledData"] = "Energy.Active.Import.Register,Power.Active.Import,Voltage,Current.Import,SoC",
        ["StopTxnSampledData"] = "Energy.Active.Import.Register",
        ["ConnectionTimeOut"] = "60",
        ["StopTransactionOnEVSideDisconnect"] = "true",
        ["StopTransactionOnInvalidId"] = "true",
        ["AuthorizeRemoteTxRequests"] = "true",
        ["LocalPreAuthorize"] = "true",
        ["LocalAuthorizeOffline"] = "true",
    };

    public AutoConfigurationEventHandler(
        IOcppMessageDispatcher dispatcher,
        ILogger<AutoConfigurationEventHandler> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task HandleEventAsync(ChargePointConnectedEto eventData)
    {
        var cpId = eventData.ChargePointId;
        _logger.LogInformation("Auto-configuring charge point {ChargePointId}", cpId);

        foreach (var (key, value) in StandardConfig)
        {
            try
            {
                var payload = new JObject
                {
                    ["key"] = key,
                    ["value"] = value
                };

                var result = await _dispatcher.SendCommandAsync(
                    cpId, "ChangeConfiguration", payload);

                var status = result.Value<string>("status") ?? "Unknown";
                _logger.LogInformation(
                    "Config {Key}={Value} → {Status} for {ChargePointId}",
                    key, value, status, cpId);

                if (status == "RebootRequired")
                {
                    _logger.LogWarning(
                        "Config {Key} requires reboot on {ChargePointId}", key, cpId);
                }

                await Task.Delay(200); // Avoid overwhelming charger
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to set config {Key} for {ChargePointId}", key, cpId);
            }
        }

        _logger.LogInformation("Auto-configuration completed for {ChargePointId}", cpId);
    }
}
