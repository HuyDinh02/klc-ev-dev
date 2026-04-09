using System;
using System.Text.Json;
using System.Threading.Tasks;
using KLC.Auditing;
using KLC.Hubs;
using KLC.Ocpp.Messages;
using KLC.Ocpp.Vendors;
using Microsoft.Extensions.Logging;
using Volo.Abp.Settings;

namespace KLC.Ocpp.Handlers;

public class BootNotificationHandler : IOcppActionHandler
{
    private readonly ILogger<BootNotificationHandler> _logger;
    private readonly IOcppService _ocppService;
    private readonly IMonitoringNotifier _notifier;
    private readonly VendorProfileFactory _vendorProfileFactory;
    private readonly IAuditEventLogger _auditLogger;
    private readonly ISettingProvider _settingProvider;

    public string Action => "BootNotification";

    public BootNotificationHandler(
        ILogger<BootNotificationHandler> logger,
        IOcppService ocppService,
        IMonitoringNotifier notifier,
        VendorProfileFactory vendorProfileFactory,
        IAuditEventLogger auditLogger,
        ISettingProvider settingProvider)
    {
        _logger = logger;
        _ocppService = ocppService;
        _notifier = notifier;
        _vendorProfileFactory = vendorProfileFactory;
        _auditLogger = auditLogger;
        _settingProvider = settingProvider;
    }

    public async Task<string> HandleAsync(OcppHandlerContext context)
    {
        var request = JsonSerializer.Deserialize<BootNotificationRequest>(context.Payload.GetRawText());
        if (request == null)
            return context.Parser.SerializeCallError(context.UniqueId, OcppErrorCode.FormationViolation, "Invalid BootNotification payload");

        _logger.LogInformation("BootNotification from {ChargePointId}: Vendor={Vendor}, Model={Model}, FW={FirmwareVersion}",
            context.Connection.ChargePointId, request.ChargePointVendor, request.ChargePointModel, request.FirmwareVersion);

        // Detect vendor profile from BootNotification vendor/model strings
        var vendorProfile = _vendorProfileFactory.Detect(request.ChargePointVendor, request.ChargePointModel);
        context.Connection.SetVendorProfile(vendorProfile.ProfileType);

        _logger.LogInformation("Vendor profile for {ChargePointId}: {VendorProfile}",
            context.Connection.ChargePointId, vendorProfile.ProfileType);

        // Capture previous status before BootNotification changes it
        var stationBefore = await _ocppService.GetStationByChargePointIdAsync(context.Connection.ChargePointId);
        var previousStationStatus = stationBefore?.Status;

        // Persist to database (including vendor profile)
        var stationId = await _ocppService.HandleBootNotificationAsync(
            context.Connection.ChargePointId,
            request.ChargePointVendor ?? string.Empty,
            request.ChargePointModel ?? string.Empty,
            request.ChargePointSerialNumber,
            request.FirmwareVersion);

        // Persist vendor profile on station entity and notify status change
        if (stationId.HasValue)
        {
            var station = await _ocppService.GetStationByChargePointIdAsync(context.Connection.ChargePointId);
            if (station != null)
            {
                if (station.VendorProfile != vendorProfile.ProfileType)
                {
                    station.SetVendorProfile(vendorProfile.ProfileType);
                }
                station.SetVendorProfileName(vendorProfile.ProfileType.ToString());

                // Broadcast station status change via SignalR (Offline -> Online)
                if (previousStationStatus.HasValue && previousStationStatus.Value != station.Status)
                {
                    await _notifier.NotifyStationStatusChangedAsync(
                        station.Id,
                        station.Name,
                        previousStationStatus.Value,
                        station.Status);
                }
            }
        }

        context.Connection.RecordHeartbeat();

        // Register the station ID on the connection for SignalR notifications
        if (stationId.HasValue)
        {
            context.Connection.SetRegistered(stationId.Value);
            context.Connection.PendingPostBootConfig = true;
        }

        // Reject unknown stations (BR-006-02)
        var status = stationId.HasValue ? RegistrationStatus.Accepted : RegistrationStatus.Rejected;

        _auditLogger.LogOcppEvent("BootNotification", context.Connection.ChargePointId,
            $"Vendor={request.ChargePointVendor}, Model={request.ChargePointModel}, Status={status}");

        var heartbeatSetting = await _settingProvider.GetOrNullAsync(Settings.KLCSettings.Ocpp.HeartbeatInterval);
        var heartbeatInterval = int.TryParse(heartbeatSetting, out var parsed) && parsed > 0
            ? parsed
            : vendorProfile.HeartbeatIntervalSeconds;

        // Use vendor profile timezone if set, otherwise UTC (OCPP standard)
        var bootTz = vendorProfile.ResponseTimezone ?? TimeZoneInfo.Utc;
        var bootResponseTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bootTz);

        var response = new BootNotificationResponse
        {
            Status = status,
            CurrentTime = bootResponseTime.ToString("o"),
            Interval = heartbeatInterval
        };

        return context.Parser.SerializeCallResult(context.UniqueId, response);
    }
}
