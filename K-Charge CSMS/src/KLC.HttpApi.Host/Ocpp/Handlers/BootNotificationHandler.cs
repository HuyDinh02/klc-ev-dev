using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Uow;
using KLC.ChargingStations;
using KLC.Ocpp.Events;

namespace KLC.Ocpp.Handlers;

/// <summary>
/// Handles BootNotification messages from charge points.
/// Registers the charge point hardware info and marks it as online.
/// </summary>
public class BootNotificationHandler : IOcppMessageHandler
{
    private readonly IChargingStationRepository _stationRepository;
    private readonly IDistributedEventBus _eventBus;
    private readonly IUnitOfWorkManager _uowManager;
    private readonly ILogger<BootNotificationHandler> _logger;
    private readonly IGuidGenerator _guidGenerator;

    public BootNotificationHandler(
        IChargingStationRepository stationRepository,
        IDistributedEventBus eventBus,
        IUnitOfWorkManager uowManager,
        ILogger<BootNotificationHandler> logger,
        IGuidGenerator guidGenerator)
    {
        _stationRepository = stationRepository;
        _eventBus = eventBus;
        _uowManager = uowManager;
        _logger = logger;
        _guidGenerator = guidGenerator;
    }

    public async Task<JObject> HandleAsync(
        string chargePointId,
        JObject payload,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract payload fields
            var chargePointVendor = payload.Value<string>("chargePointVendor");
            var chargePointModel = payload.Value<string>("chargePointModel");
            var chargePointSerialNumber = payload.Value<string?>("chargePointSerialNumber");
            var firmwareVersion = payload.Value<string?>("firmwareVersion");
            var iccid = payload.Value<string?>("iccid");
            var imsi = payload.Value<string?>("imsi");

            if (string.IsNullOrWhiteSpace(chargePointVendor) || string.IsNullOrWhiteSpace(chargePointModel))
            {
                _logger.LogWarning("BootNotification missing required fields for charge point {ChargePointId}",
                    chargePointId);
                return new JObject
                {
                    { "status", "Pending" },
                    { "currentTime", DateTime.UtcNow.ToString("O") },
                    { "interval", 300 }
                };
            }

            using var uow = _uowManager.Begin();

            // Find existing station
            var station = await _stationRepository.FindByChargePointIdAsync(
                chargePointId,
                includeDetails: true,
                cancellationToken: cancellationToken);

            if (station == null)
            {
                _logger.LogInformation(
                    "BootNotification received from unknown charge point {ChargePointId}. Vendor: {Vendor}, Model: {Model}",
                    chargePointId, chargePointVendor, chargePointModel);

                await uow.CompleteAsync();
                return new JObject
                {
                    { "status", "Pending" },
                    { "currentTime", DateTime.UtcNow.ToString("O") },
                    { "interval", 300 }
                };
            }

            // Update station boot info and mark online
            station.UpdateBootInfo(
                chargePointVendor,
                chargePointModel,
                chargePointSerialNumber,
                firmwareVersion,
                iccid,
                imsi);
            station.SetOnline();

            await _stationRepository.UpdateAsync(station, autoSave: true, cancellationToken: cancellationToken);
            await uow.CompleteAsync();

            _logger.LogInformation(
                "BootNotification processed for {ChargePointId}. Vendor: {Vendor}, Model: {Model}, Firmware: {Firmware}",
                chargePointId, chargePointVendor, chargePointModel, firmwareVersion ?? "N/A");

            // Publish event
            await _eventBus.PublishAsync(new ChargePointConnectedEto
            {
                ChargePointId = chargePointId,
                Vendor = chargePointVendor,
                Model = chargePointModel,
                FirmwareVersion = firmwareVersion,
                Timestamp = DateTime.UtcNow
            }, cancellationToken: cancellationToken);

            return new JObject
            {
                { "status", "Accepted" },
                { "currentTime", DateTime.UtcNow.ToString("O") },
                { "interval", 300 }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing BootNotification for charge point {ChargePointId}", chargePointId);
            return new JObject
            {
                { "status", "Pending" },
                { "currentTime", DateTime.UtcNow.ToString("O") },
                { "interval", 300 }
            };
        }
    }
}
