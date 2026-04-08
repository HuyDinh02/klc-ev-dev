using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KLC.Settings;
using KLC.Stations;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Settings;

namespace KLC.Ocpp;

/// <summary>
/// Sends ChangeConfiguration commands to a charger after its BootNotification is accepted.
/// Reads desired configuration values from ABP settings and pushes them to the charger
/// via OcppConnection.SendCallAsync (no dependency on OcppRemoteCommandService).
/// Also syncs connector power info from the charger's OCPP configuration to the DB.
/// </summary>
public class OcppPostBootConfigService
{
    private readonly ISettingProvider _settingProvider;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<Connector, Guid> _connectorRepository;
    private readonly ILogger<OcppPostBootConfigService> _logger;
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(10);

    public OcppPostBootConfigService(
        ISettingProvider settingProvider,
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<Connector, Guid> connectorRepository,
        ILogger<OcppPostBootConfigService> logger)
    {
        _settingProvider = settingProvider;
        _stationRepository = stationRepository;
        _connectorRepository = connectorRepository;
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
            _logger.LogInformation("Post-boot auto-configuration disabled for {ChargePointId}", connection.ChargePointId);
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
                    _logger.LogInformation("Skipping {OcppKey} for {ChargePointId}: setting value is empty",
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
                    _logger.LogInformation("ChangeConfiguration({OcppKey}={Value}) accepted by {ChargePointId}",
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

        // Trigger StatusNotification for all connectors to update their status in our DB.
        // After deploy/reconnect, connector status may be stale (Unavailable) even though
        // the charger's physical connectors are Available.
        try
        {
            await connection.SendCallAsync("TriggerMessage",
                new { requestedMessage = "StatusNotification" }, CommandTimeout);
            _logger.LogInformation("TriggerMessage(StatusNotification) sent to {ChargePointId}", connection.ChargePointId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TriggerMessage(StatusNotification) failed for {ChargePointId}",
                connection.ChargePointId);
        }

        // Sync connector power info from charger's OCPP configuration
        await SyncConnectorPowerAsync(connection);
    }

    /// <summary>
    /// Query the charger's configuration for power-related keys and sync
    /// connector MaxPowerKw in our DB to match the real hardware capabilities.
    /// </summary>
    private async Task SyncConnectorPowerAsync(OcppConnection connection)
    {
        if (!connection.StationId.HasValue) return;

        try
        {
            // Read all configuration from charger
            var response = await connection.SendCallAsync(
                "GetConfiguration", new { }, CommandTimeout);

            if (response == null || response.StartsWith("ERROR:"))
            {
                _logger.LogWarning("GetConfiguration failed for {ChargePointId} during power sync", connection.ChargePointId);
                return;
            }

            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (!root.TryGetProperty("configurationKey", out var keysArray))
                return;

            // Build a dictionary of all config keys
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in keysArray.EnumerateArray())
            {
                var key = entry.TryGetProperty("key", out var k) ? k.GetString() ?? "" : "";
                var value = entry.TryGetProperty("value", out var v) ? v.GetString() ?? "" : "";
                if (!string.IsNullOrEmpty(key))
                    config[key] = value;
            }

            _logger.LogInformation(
                "POWER_SYNC: GetConfiguration from {ChargePointId} returned {Count} keys",
                connection.ChargePointId, config.Count);

            // Extract number of connectors
            var numberOfConnectors = 0;
            if (config.TryGetValue("NumberOfConnectors", out var numConn))
                int.TryParse(numConn, out numberOfConnectors);

            // Extract power info from various OCPP keys
            // Different chargers use different keys for max power/current
            decimal? maxCurrentAmps = null;
            decimal? voltageV = null;
            decimal? maxPowerW = null;

            // Standard OCPP keys
            if (config.TryGetValue("MaxChargingCurrent", out var maxCurr) && decimal.TryParse(maxCurr, out var mc))
                maxCurrentAmps = mc;
            if (config.TryGetValue("SupplyVoltage", out var voltage) && decimal.TryParse(voltage, out var sv))
                voltageV = sv;

            // Vendor-specific keys (ChargeCore, ABB, etc.)
            foreach (var key in config.Keys)
            {
                var lowerKey = key.ToLowerInvariant();
                if (lowerKey.Contains("maxpower") || lowerKey.Contains("max_power") || lowerKey.Contains("ratedpower"))
                {
                    if (decimal.TryParse(config[key], out var pw))
                    {
                        // Determine if value is in W or kW (>1000 = likely Watts)
                        maxPowerW = pw > 1000 ? pw : pw * 1000;
                        _logger.LogInformation("POWER_SYNC: Found power key {Key}={Value} for {ChargePointId}",
                            key, config[key], connection.ChargePointId);
                    }
                }
            }

            // Calculate power from current × voltage if direct power key not available
            if (maxPowerW == null && maxCurrentAmps.HasValue && voltageV.HasValue)
            {
                maxPowerW = maxCurrentAmps.Value * voltageV.Value;
            }

            var maxPowerKw = maxPowerW.HasValue ? Math.Round(maxPowerW.Value / 1000m, 1) : (decimal?)null;

            if (maxPowerKw == null && numberOfConnectors == 0)
            {
                _logger.LogInformation("POWER_SYNC: No power or connector info found for {ChargePointId}", connection.ChargePointId);
                return;
            }

            // Update connectors in DB
            var station = await _stationRepository.FirstOrDefaultAsync(s => s.Id == connection.StationId.Value);
            if (station == null) return;

            var connectors = await _connectorRepository.GetListAsync(c => c.StationId == station.Id);

            var updated = false;
            foreach (var connector in connectors)
            {
                if (maxPowerKw.HasValue && maxPowerKw.Value > 0 && connector.MaxPowerKw != maxPowerKw.Value)
                {
                    _logger.LogInformation(
                        "POWER_SYNC: Updating connector #{ConnectorNumber} on {ChargePointId}: " +
                        "MaxPower {OldPower}kW → {NewPower}kW",
                        connector.ConnectorNumber, connection.ChargePointId,
                        connector.MaxPowerKw, maxPowerKw.Value);

                    connector.SetMaxPower(maxPowerKw.Value);
                    await _connectorRepository.UpdateAsync(connector);
                    updated = true;
                }
            }

            if (updated)
            {
                _logger.LogInformation("POWER_SYNC: Connector power synced for {ChargePointId}", connection.ChargePointId);
            }
            else
            {
                _logger.LogInformation("POWER_SYNC: No connector power changes needed for {ChargePointId}", connection.ChargePointId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "POWER_SYNC: Failed to sync connector power for {ChargePointId}", connection.ChargePointId);
        }
    }
}
