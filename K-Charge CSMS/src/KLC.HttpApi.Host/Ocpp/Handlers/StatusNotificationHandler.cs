using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Uow;
using KLC.ChargingStations;
using KLC.Faults;
using KLC.Ocpp;
using KLC.Ocpp.Events;

namespace KLC.Ocpp.Handlers;

/// <summary>
/// Handles StatusNotification messages from charge points.
/// Updates connector or station status, records faults if error code is present.
/// </summary>
public class StatusNotificationHandler : IOcppMessageHandler
{
    private readonly IChargingStationRepository _stationRepository;
    private readonly IRepository<Fault, Guid> _faultRepository;
    private readonly IDistributedEventBus _eventBus;
    private readonly IUnitOfWorkManager _uowManager;
    private readonly ILogger<StatusNotificationHandler> _logger;
    private readonly IGuidGenerator _guidGenerator;

    public StatusNotificationHandler(
        IChargingStationRepository stationRepository,
        IRepository<Fault, Guid> faultRepository,
        IDistributedEventBus eventBus,
        IUnitOfWorkManager uowManager,
        ILogger<StatusNotificationHandler> logger,
        IGuidGenerator guidGenerator)
    {
        _stationRepository = stationRepository;
        _faultRepository = faultRepository;
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
            var connectorId = payload.Value<int?>("connectorId") ?? 0;
            var statusStr = payload.Value<string>("status") ?? "Unavailable";
            var errorCodeStr = payload.Value<string>("errorCode") ?? "NoError";
            var timestampStr = payload.Value<string>("timestamp");
            var info = payload.Value<string?>("info");
            var vendorErrorCode = payload.Value<string?>("vendorErrorCode");

            // Parse enum values
            if (!Enum.TryParse<ChargePointStatus>(statusStr, ignoreCase: true, out var status))
            {
                status = ChargePointStatus.Unavailable;
                _logger.LogWarning("Invalid status value {Status} for charge point {ChargePointId}",
                    statusStr, chargePointId);
            }

            if (!Enum.TryParse<ChargePointErrorCode>(errorCodeStr, ignoreCase: true, out var errorCode))
            {
                errorCode = ChargePointErrorCode.NoError;
                _logger.LogWarning("Invalid error code value {ErrorCode} for charge point {ChargePointId}",
                    errorCodeStr, chargePointId);
            }

            var timestamp = DateTime.TryParse(timestampStr, out var parsed)
                ? parsed
                : DateTime.UtcNow;

            using var uow = _uowManager.Begin();

            // Find station
            var station = await _stationRepository.FindByChargePointIdAsync(
                chargePointId,
                includeDetails: true,
                cancellationToken: cancellationToken);

            if (station == null)
            {
                _logger.LogWarning("StatusNotification received from unknown charge point {ChargePointId}",
                    chargePointId);
                await uow.CompleteAsync();
                return new JObject();
            }

            if (connectorId == 0)
            {
                // Update station overall status
                station.UpdateStatus(status);
                _logger.LogInformation(
                    "Station {ChargePointId} status updated to {Status}",
                    chargePointId, status);
            }
            else
            {
                // Update specific connector status
                var connector = station.GetConnector(connectorId);
                if (connector != null)
                {
                    connector.UpdateStatus(status, errorCode, timestamp);
                    _logger.LogInformation(
                        "Connector {ConnectorId} on station {ChargePointId} status updated to {Status}",
                        connectorId, chargePointId, status);
                }
                else
                {
                    _logger.LogWarning(
                        "StatusNotification for unknown connector {ConnectorId} on station {ChargePointId}",
                        connectorId, chargePointId);
                }
            }

            // Create fault record if error detected
            if (errorCode != ChargePointErrorCode.NoError)
            {
                var fault = new Fault(
                    _guidGenerator.Create(),
                    station.Id,
                    connectorId,
                    errorCode.ToString(),
                    timestamp,
                    info,
                    vendorErrorCode);

                await _faultRepository.InsertAsync(fault, autoSave: true, cancellationToken: cancellationToken);
                _logger.LogWarning(
                    "Fault recorded for charge point {ChargePointId}, connector {ConnectorId}: {ErrorCode}",
                    chargePointId, connectorId, errorCode);
            }

            await _stationRepository.UpdateAsync(station, autoSave: true, cancellationToken: cancellationToken);
            await uow.CompleteAsync();

            // Publish ConnectorStatusChangedEto
            await _eventBus.PublishAsync(new ConnectorStatusChangedEto
            {
                ChargePointId = chargePointId,
                ConnectorId = connectorId,
                Status = status.ToString(),
                ErrorCode = errorCode.ToString(),
                Timestamp = timestamp
            }, cancellationToken: cancellationToken);

            // Publish FaultDetectedEto if error
            if (errorCode != ChargePointErrorCode.NoError)
            {
                await _eventBus.PublishAsync(new FaultDetectedEto
                {
                    ChargePointId = chargePointId,
                    ConnectorId = connectorId,
                    ErrorCode = errorCode.ToString(),
                    Info = info,
                    Timestamp = timestamp
                }, cancellationToken: cancellationToken);
            }

            return new JObject();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing StatusNotification for charge point {ChargePointId}",
                chargePointId);
            return new JObject();
        }
    }
}
