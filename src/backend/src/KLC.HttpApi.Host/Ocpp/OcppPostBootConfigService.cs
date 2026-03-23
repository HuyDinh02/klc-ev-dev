using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Settings;
using Microsoft.Extensions.Logging;
using Volo.Abp.Settings;

namespace KLC.Ocpp;

/// <summary>
/// Sends ChangeConfiguration commands to a charger after its BootNotification is accepted.
/// Reads desired configuration values from ABP settings and pushes them to the charger
/// via OcppConnection.SendCallAsync (no dependency on OcppRemoteCommandService).
/// </summary>
public class OcppPostBootConfigService
{
    private readonly ISettingProvider _settingProvider;
    private readonly ILogger<OcppPostBootConfigService> _logger;
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(10);

    public OcppPostBootConfigService(
        ISettingProvider settingProvider,
        ILogger<OcppPostBootConfigService> logger)
    {
        _settingProvider = settingProvider;
        _logger = logger;
    }

    /// <summary>
    /// Send ChangeConfiguration commands for all managed OCPP keys.
    /// Errors on individual keys are logged and skipped so the remaining keys are still configured.
    /// </summary>
    public async Task SendPostBootConfigurationAsync(OcppConnection connection)
    {
        var autoConfigSetting = await _settingProvider.GetOrNullAsync(KLCSettings.Ocpp.AutoConfigOnBoot);
        if (!string.Equals(autoConfigSetting, "true", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Post-boot auto-configuration disabled for {ChargePointId}", connection.ChargePointId);
            return;
        }

        _logger.LogInformation("Sending post-boot configuration to {ChargePointId}", connection.ChargePointId);

        // Build the list of OCPP keys to configure from settings
        var configEntries = new List<(string OcppKey, string SettingKey)>
        {
            ("HeartbeatInterval", KLCSettings.Ocpp.HeartbeatInterval),
            ("MeterValueSampleInterval", KLCSettings.Ocpp.MeterValueInterval),
            ("ClockAlignedDataInterval", KLCSettings.Ocpp.ClockAlignedDataInterval),
            ("MeterValuesSampledData", KLCSettings.Ocpp.MeterValuesSampledData),
            ("StopTxnSampledData", KLCSettings.Ocpp.StopTxnSampledData),
            ("StopTransactionOnEVSideDisconnect", KLCSettings.Ocpp.StopTransactionOnEVSideDisconnect),
        };

        var successCount = 0;
        var failCount = 0;

        foreach (var (ocppKey, settingKey) in configEntries)
        {
            try
            {
                var value = await _settingProvider.GetOrNullAsync(settingKey);
                if (string.IsNullOrEmpty(value))
                {
                    _logger.LogDebug("Skipping {OcppKey} for {ChargePointId}: setting value is empty",
                        ocppKey, connection.ChargePointId);
                    continue;
                }

                var response = await connection.SendCallAsync(
                    "ChangeConfiguration",
                    new { key = ocppKey, value },
                    CommandTimeout);

                if (response == null)
                {
                    _logger.LogWarning("ChangeConfiguration({OcppKey}) timed out for {ChargePointId}",
                        ocppKey, connection.ChargePointId);
                    failCount++;
                }
                else if (response.StartsWith("ERROR:"))
                {
                    _logger.LogWarning("ChangeConfiguration({OcppKey}) error for {ChargePointId}: {Response}",
                        ocppKey, connection.ChargePointId, response);
                    failCount++;
                }
                else
                {
                    _logger.LogDebug("ChangeConfiguration({OcppKey}={Value}) accepted by {ChargePointId}",
                        ocppKey, value, connection.ChargePointId);
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send ChangeConfiguration({OcppKey}) to {ChargePointId}",
                    ocppKey, connection.ChargePointId);
                failCount++;
            }
        }

        _logger.LogInformation(
            "Post-boot configuration completed for {ChargePointId}: {SuccessCount} succeeded, {FailCount} failed",
            connection.ChargePointId, successCount, failCount);
    }
}
