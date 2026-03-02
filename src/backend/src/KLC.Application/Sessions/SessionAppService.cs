using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Permissions;
using KLC.Stations;
using KLC.Tariffs;
using KLC.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace KLC.Sessions;

[Authorize]
public class SessionAppService : KLCAppService, ISessionAppService
{
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<Connector, Guid> _connectorRepository;
    private readonly IRepository<Vehicle, Guid> _vehicleRepository;
    private readonly IRepository<TariffPlan, Guid> _tariffRepository;
    private readonly IRepository<MeterValue, Guid> _meterValueRepository;

    public SessionAppService(
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<Connector, Guid> connectorRepository,
        IRepository<Vehicle, Guid> vehicleRepository,
        IRepository<TariffPlan, Guid> tariffRepository,
        IRepository<MeterValue, Guid> meterValueRepository)
    {
        _sessionRepository = sessionRepository;
        _stationRepository = stationRepository;
        _connectorRepository = connectorRepository;
        _vehicleRepository = vehicleRepository;
        _tariffRepository = tariffRepository;
        _meterValueRepository = meterValueRepository;
    }

    public async Task<ChargingSessionDto> StartAsync(StartSessionDto input)
    {
        var userId = CurrentUser.GetId();

        // Check for existing active session (BR-010-03)
        var existingSession = await _sessionRepository.FirstOrDefaultAsync(
            s => s.UserId == userId &&
                 (s.Status == SessionStatus.Pending || s.Status == SessionStatus.InProgress));
        if (existingSession != null)
        {
            throw new BusinessException("MOD_010_002");
        }

        // Get station and validate
        var station = await _stationRepository.GetAsync(input.StationId);
        var connectors = await _connectorRepository.GetListAsync(c => c.StationId == input.StationId);
        var connector = connectors.FirstOrDefault(c => c.ConnectorNumber == input.ConnectorNumber);

        if (connector == null)
        {
            throw new BusinessException("MOD_010_001");
        }

        if (connector.Status != ConnectorStatus.Available)
        {
            throw new BusinessException("MOD_010_001")
                .WithData("status", connector.Status);
        }

        // Get or validate vehicle
        Guid? vehicleId = input.VehicleId;
        if (!vehicleId.HasValue)
        {
            var defaultVehicle = await _vehicleRepository.FirstOrDefaultAsync(
                v => v.UserId == userId && v.IsActive && v.IsDefault);
            if (defaultVehicle == null)
            {
                throw new BusinessException("MOD_010_003");
            }
            vehicleId = defaultVehicle.Id;
        }

        // Get tariff plan
        var tariff = station.TariffPlanId.HasValue
            ? await _tariffRepository.GetAsync(station.TariffPlanId.Value)
            : await _tariffRepository.FirstOrDefaultAsync(t => t.IsDefault && t.IsActive);

        var ratePerKwh = tariff?.GetTotalRatePerKwh() ?? 0;

        // Create session
        var session = new ChargingSession(
            GuidGenerator.Create(),
            userId,
            input.StationId,
            input.ConnectorNumber,
            vehicleId,
            tariff?.Id,
            ratePerKwh
        );

        session.MarkStarting();

        await _sessionRepository.InsertAsync(session);

        // TODO: Send RemoteStartTransaction via OCPP

        return await MapToDtoAsync(session);
    }

    public async Task<ChargingSessionDto> StopAsync(Guid id, StopSessionDto? input = null)
    {
        var userId = CurrentUser.GetId();
        var session = await _sessionRepository.GetAsync(id);

        if (session.UserId != userId)
        {
            throw new BusinessException("MOD_010_005");
        }

        if (session.Status != SessionStatus.InProgress)
        {
            throw new BusinessException("MOD_010_005");
        }

        session.MarkStopping();
        await _sessionRepository.UpdateAsync(session);

        // TODO: Send RemoteStopTransaction via OCPP

        return await MapToDtoAsync(session);
    }

    public async Task<ChargingSessionDto> GetAsync(Guid id)
    {
        var session = await _sessionRepository.GetAsync(id);
        return await MapToDtoAsync(session);
    }

    public async Task<ActiveSessionDto?> GetActiveSessionAsync()
    {
        var userId = CurrentUser.GetId();
        var session = await _sessionRepository.FirstOrDefaultAsync(
            s => s.UserId == userId && s.Status == SessionStatus.InProgress);

        if (session == null) return null;

        var station = await _stationRepository.GetAsync(session.StationId);

        // Get latest meter values
        var meterValues = await _meterValueRepository.GetListAsync(
            mv => mv.SessionId == session.Id);
        var latestMeter = meterValues.OrderByDescending(mv => mv.Timestamp).FirstOrDefault();

        return new ActiveSessionDto
        {
            SessionId = session.Id,
            StationName = station.Name,
            StationAddress = station.Address,
            ConnectorNumber = session.ConnectorNumber,
            StartTime = session.StartTime ?? session.CreationTime,
            CurrentEnergyKwh = latestMeter?.EnergyKwh ?? session.TotalEnergyKwh,
            EstimatedCost = (latestMeter?.EnergyKwh ?? session.TotalEnergyKwh) * session.RatePerKwh,
            CurrentPowerKw = latestMeter?.PowerKw
        };
    }

    public async Task<PagedResultDto<SessionListDto>> GetHistoryAsync(GetSessionListDto input)
    {
        var userId = CurrentUser.GetId();
        return await GetSessionsInternalAsync(input, userId);
    }

    [Authorize(KLCPermissions.Sessions.ViewAll)]
    public async Task<PagedResultDto<SessionListDto>> GetAllSessionsAsync(GetSessionListDto input)
    {
        return await GetSessionsInternalAsync(input, null);
    }

    public async Task<List<MeterValueDto>> GetMeterValuesAsync(Guid sessionId)
    {
        var meterValues = await _meterValueRepository.GetListAsync(mv => mv.SessionId == sessionId);

        return meterValues
            .OrderBy(mv => mv.Timestamp)
            .Select(mv => new MeterValueDto
            {
                Id = mv.Id,
                Timestamp = mv.Timestamp,
                EnergyKwh = mv.EnergyKwh,
                CurrentAmps = mv.CurrentAmps,
                VoltageVolts = mv.VoltageVolts,
                PowerKw = mv.PowerKw,
                SocPercent = mv.SocPercent
            })
            .ToList();
    }

    private async Task<PagedResultDto<SessionListDto>> GetSessionsInternalAsync(GetSessionListDto input, Guid? userId)
    {
        var query = await _sessionRepository.GetQueryableAsync();

        if (userId.HasValue)
        {
            query = query.Where(s => s.UserId == userId.Value);
        }

        if (input.Status.HasValue)
        {
            query = query.Where(s => s.Status == input.Status.Value);
        }

        if (input.StationId.HasValue)
        {
            query = query.Where(s => s.StationId == input.StationId.Value);
        }

        if (input.FromDate.HasValue)
        {
            query = query.Where(s => s.CreationTime >= input.FromDate.Value);
        }

        if (input.ToDate.HasValue)
        {
            query = query.Where(s => s.CreationTime <= input.ToDate.Value);
        }

        if (input.Cursor.HasValue)
        {
            query = query.Where(s => s.Id.CompareTo(input.Cursor.Value) > 0);
        }

        query = query.OrderByDescending(s => s.CreationTime);

        var totalCount = await AsyncExecuter.CountAsync(query);
        var sessions = await AsyncExecuter.ToListAsync(query.Take(input.MaxResultCount));

        var stationIds = sessions.Select(s => s.StationId).Distinct().ToList();
        var stations = await _stationRepository.GetListAsync(st => stationIds.Contains(st.Id));
        var stationMap = stations.ToDictionary(st => st.Id, st => st.Name);

        var dtos = sessions.Select(s => new SessionListDto
        {
            Id = s.Id,
            StationName = stationMap.TryGetValue(s.StationId, out var sName) ? sName : "Unknown",
            ConnectorNumber = s.ConnectorNumber,
            Status = s.Status,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            TotalEnergyKwh = s.TotalEnergyKwh,
            TotalCost = s.TotalCost
        }).ToList();

        return new PagedResultDto<SessionListDto>(totalCount, dtos);
    }

    private async Task<ChargingSessionDto> MapToDtoAsync(ChargingSession session)
    {
        var station = await _stationRepository.GetAsync(session.StationId);
        string? vehicleName = null;

        if (session.VehicleId.HasValue)
        {
            var vehicle = await _vehicleRepository.FirstOrDefaultAsync(v => v.Id == session.VehicleId.Value);
            vehicleName = vehicle != null ? $"{vehicle.Make} {vehicle.Model}" : null;
        }

        return new ChargingSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            VehicleId = session.VehicleId,
            StationId = session.StationId,
            ConnectorNumber = session.ConnectorNumber,
            OcppTransactionId = session.OcppTransactionId,
            Status = session.Status,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            MeterStart = session.MeterStart,
            MeterStop = session.MeterStop,
            TotalEnergyKwh = session.TotalEnergyKwh,
            TotalCost = session.TotalCost,
            TariffPlanId = session.TariffPlanId,
            RatePerKwh = session.RatePerKwh,
            StopReason = session.StopReason,
            IdTag = session.IdTag,
            StationName = station.Name,
            VehicleName = vehicleName,
            CreationTime = session.CreationTime,
            CreatorId = session.CreatorId,
            LastModificationTime = session.LastModificationTime,
            LastModifierId = session.LastModifierId
        };
    }
}
