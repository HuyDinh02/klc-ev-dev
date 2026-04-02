using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.Sessions;
using KLC.Stations;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace KLC.Ocpp;

/// <summary>
/// Handles OCPP MeterValues messages: validates, persists, and updates session energy totals.
/// </summary>
public class OcppMeterValuesHandler : DomainService
{
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<Connector, Guid> _connectorRepository;
    private readonly ILogger<OcppMeterValuesHandler> _logger;

    public OcppMeterValuesHandler(
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<Connector, Guid> connectorRepository,
        ILogger<OcppMeterValuesHandler> logger)
    {
        _stationRepository = stationRepository;
        _sessionRepository = sessionRepository;
        _connectorRepository = connectorRepository;
        _logger = logger;
    }

    public async Task<MeterValuesResult?> HandleMeterValuesAsync(
        string chargePointId,
        int connectorId,
        int? transactionId,
        decimal energyWh,
        string? timestamp,
        decimal? currentAmps,
        decimal? voltage,
        decimal? power,
        decimal? soc)
    {
        ChargingSession? session = null;

        if (transactionId.HasValue)
        {
            // Use WithDetailsAsync to load MeterValues navigation property
            var query = await _sessionRepository.WithDetailsAsync(s => s.MeterValues);
            session = (await AsyncExecuter.ToListAsync(
                query.Where(s => s.OcppTransactionId == transactionId.Value))).FirstOrDefault();
        }

        var station = await _stationRepository.FirstOrDefaultAsync(s => s.StationCode == chargePointId);
        if (station == null)
        {
            _logger.LogWarning("MeterValues from unknown station: {ChargePointId}", chargePointId);
            return null;
        }

        // Convert Wh to kWh
        var energyKwh = Math.Round(energyWh / 1000m, 3);

        // Parse OCPP timestamp, fall back to UTC now
        var meterTimestamp = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(timestamp) && DateTime.TryParse(timestamp, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
        {
            meterTimestamp = parsed.ToUniversalTime();
        }

        if (session != null)
        {
            // Monotonic validation: reject backward readings
            if (session.MeterStart.HasValue && energyWh < session.MeterStart.Value)
            {
                _logger.LogWarning(
                    "Rejected MeterValue for session {SessionId}: energyWh {EnergyWh} < MeterStart {MeterStart}",
                    session.Id, energyWh, session.MeterStart.Value);
                return null;
            }

            // Monotonic validation: reject non-monotonic readings
            var lastReading = session.MeterValues
                .OrderByDescending(mv => mv.Timestamp)
                .FirstOrDefault();
            if (lastReading != null && energyKwh < lastReading.EnergyKwh)
            {
                _logger.LogWarning(
                    "Rejected non-monotonic MeterValue for session {SessionId}: {EnergyKwh}kWh < last {LastKwh}kWh",
                    session.Id, energyKwh, lastReading.EnergyKwh);
                return null;
            }

            // Reject unreasonable jumps (> 500 kWh delta from last reading)
            if (lastReading != null && (energyKwh - lastReading.EnergyKwh) > 500m)
            {
                _logger.LogWarning(
                    "Rejected unreasonable MeterValue jump for session {SessionId}: {EnergyKwh}kWh, last={LastKwh}kWh",
                    session.Id, energyKwh, lastReading.EnergyKwh);
                return null;
            }

            // Add meter value to session (returns null if duplicate)
            var powerKw = power != null ? power / 1000m : null; // Convert W to kW
            var meterValue = session.AddMeterValue(
                GuidGenerator.Create(),
                energyKwh,
                meterTimestamp,
                currentAmps,
                voltage,
                powerKw,
                soc
            );

            if (meterValue == null)
            {
                _logger.LogDebug("Duplicate MeterValue skipped for session {SessionId}", session.Id);
                return null;
            }

            // Update running energy total during charging
            if (session.MeterStart.HasValue && energyWh > 0)
            {
                var totalEnergyKwh = Math.Round((energyWh - session.MeterStart.Value) / 1000m, 3);
                if (totalEnergyKwh > 0)
                {
                    session.UpdateTotalEnergy(totalEnergyKwh);
                }
            }

            await _sessionRepository.UpdateAsync(session);

            _logger.LogInformation("MeterValue recorded for session {SessionId}: {EnergyKwh}kWh, Total={TotalKwh}kWh",
                session.Id, energyKwh, session.TotalEnergyKwh);

            return new MeterValuesResult(
                session.Id,
                session.StationId,
                session.ConnectorNumber,
                session.TotalEnergyKwh,
                session.TotalCost,
                powerKw,
                soc);
        }
        else
        {
            // Store standalone meter value (no active session)
            _logger.LogDebug("MeterValue received without active session for {ChargePointId}", chargePointId);
            return null;
        }
    }
}
