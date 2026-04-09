using System;
using System.Text.Json;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Hubs;
using KLC.Ocpp.Messages;
using KLC.Ocpp.Vendors;
using Microsoft.Extensions.Logging;

namespace KLC.Ocpp.Handlers;

public class MeterValuesHandler : IOcppActionHandler
{
    private readonly ILogger<MeterValuesHandler> _logger;
    private readonly IOcppService _ocppService;
    private readonly IMonitoringNotifier _notifier;
    private readonly VendorProfileFactory _vendorProfileFactory;
    private readonly IOcppRemoteCommandService _remoteCommandService;

    public string Action => "MeterValues";

    public MeterValuesHandler(
        ILogger<MeterValuesHandler> logger,
        IOcppService ocppService,
        IMonitoringNotifier notifier,
        VendorProfileFactory vendorProfileFactory,
        IOcppRemoteCommandService remoteCommandService)
    {
        _logger = logger;
        _ocppService = ocppService;
        _notifier = notifier;
        _vendorProfileFactory = vendorProfileFactory;
        _remoteCommandService = remoteCommandService;
    }

    public async Task<string> HandleAsync(OcppHandlerContext context)
    {
        var request = JsonSerializer.Deserialize<MeterValuesRequest>(context.Payload.GetRawText());
        if (request == null)
            return context.Parser.SerializeCallError(context.UniqueId, OcppErrorCode.FormationViolation, "Invalid MeterValues payload");

        // Resolve vendor profile for this connection
        var vendorProfile = _vendorProfileFactory.Resolve(context.Connection.VendorProfileType);

        _logger.LogInformation("MeterValues from {ChargePointId}: Connector={ConnectorId}, TransactionId={TransactionId}, " +
            "Values={Count}, VendorProfile={VendorProfile}",
            context.Connection.ChargePointId, request.ConnectorId, request.TransactionId,
            request.MeterValue?.Length ?? 0, vendorProfile.ProfileType);

        // Process meter values (BR-006-05)
        if (request.MeterValue != null)
        {
            foreach (var mv in request.MeterValue)
            {
                var (energyWh, currentAmps, voltage, power, soc) = ExtractSampledValues(mv, vendorProfile);
                var parsedTimestamp = vendorProfile.ParseTimestamp(mv.Timestamp);
                var timestampStr = parsedTimestamp?.ToString("o") ?? mv.Timestamp;

                var meterResult = await _ocppService.HandleMeterValuesAsync(
                    context.Connection.ChargePointId,
                    request.ConnectorId,
                    request.TransactionId,
                    energyWh,
                    timestampStr,
                    currentAmps,
                    voltage,
                    power,
                    soc);

                // Push real-time meter update via SignalR
                if (meterResult != null)
                {
                    await _notifier.NotifySessionUpdatedAsync(
                        meterResult.SessionId,
                        meterResult.StationId,
                        meterResult.ConnectorNumber,
                        SessionStatus.InProgress,
                        meterResult.TotalEnergyKwh,
                        meterResult.TotalCost);

                    await _notifier.NotifyMeterValueReceivedAsync(
                        meterResult.SessionId,
                        meterResult.TotalEnergyKwh,
                        meterResult.PowerKw,
                        meterResult.SocPercent);

                    // Auto-stop when battery is full (SoC >= 100%)
                    if (meterResult.SocPercent >= 100m && request.TransactionId.HasValue)
                    {
                        _logger.LogInformation(
                            "Battery full (SoC={SoC}%) for session {SessionId} on {ChargePointId}. Sending RemoteStopTransaction.",
                            meterResult.SocPercent, meterResult.SessionId, context.Connection.ChargePointId);

                        var chargePointId = context.Connection.ChargePointId;
                        var ocppTxId = request.TransactionId.Value;
                        var sessionId = meterResult.SessionId;
                        var remoteCommandService = _remoteCommandService;
                        var logger = _logger;
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // Brief delay so MeterValues response is sent and receive loop is free
                                await Task.Delay(200);
                                var stopResult = await remoteCommandService.SendRemoteStopTransactionAsync(
                                    chargePointId, ocppTxId);
                                if (stopResult.Accepted)
                                    logger.LogInformation("RemoteStopTransaction accepted for full-battery session {SessionId}", sessionId);
                                else
                                    logger.LogWarning("RemoteStopTransaction rejected for full-battery session {SessionId}: {Error}", sessionId, stopResult.ErrorMessage);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Failed to send RemoteStopTransaction for full-battery session {SessionId}", sessionId);
                            }
                        });
                    }
                }
            }
        }

        return context.Parser.SerializeCallResult(context.UniqueId, new MeterValuesResponse());
    }

    private static (decimal energyWh, decimal? currentAmps, decimal? voltage, decimal? power, decimal? soc)
        ExtractSampledValues(MeterValue mv, IVendorProfile vendorProfile)
    {
        decimal energyWh = 0;
        decimal? currentAmps = null;
        decimal? voltage = null;
        decimal? power = null;
        decimal? soc = null;

        if (mv.SampledValue != null)
        {
            foreach (var sv in mv.SampledValue)
            {
                switch (sv.Measurand)
                {
                    case "Energy.Active.Import.Register":
                        energyWh = vendorProfile.NormalizeEnergyToWh(sv.Value ?? "0", sv.Unit, sv.Measurand);
                        break;
                    case "Current.Import":
                        if (decimal.TryParse(sv.Value, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var amps))
                            currentAmps = amps;
                        break;
                    case "Voltage":
                        if (decimal.TryParse(sv.Value, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var volts))
                            voltage = volts;
                        break;
                    case "Power.Active.Import":
                        power = vendorProfile.NormalizePowerToW(sv.Value ?? "0", sv.Unit);
                        break;
                    case "SoC":
                        if (decimal.TryParse(sv.Value, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var socVal))
                            soc = socVal;
                        break;
                }
            }
        }

        return (energyWh, currentAmps, voltage, power, soc);
    }
}
