using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace KLC.Stations;

[Authorize(KLCPermissions.Stations.Default)]
public class StationAppService : KLCAppService, IStationAppService
{
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly StationMapper _mapper;

    public StationAppService(IRepository<ChargingStation, Guid> stationRepository)
    {
        _stationRepository = stationRepository;
        _mapper = new StationMapper();
    }

    [Authorize(KLCPermissions.Stations.Create)]
    public async Task<StationDto> CreateAsync(CreateStationDto input)
    {
        // Check for duplicate station code
        var existingStation = await _stationRepository.FirstOrDefaultAsync(s => s.StationCode == input.StationCode);
        if (existingStation != null)
        {
            throw new BusinessException("MOD_001_001")
                .WithData("stationCode", input.StationCode);
        }

        var station = new ChargingStation(
            GuidGenerator.Create(),
            input.StationCode,
            input.Name,
            input.Address,
            input.Latitude,
            input.Longitude,
            input.StationGroupId,
            input.TariffPlanId
        );

        await _stationRepository.InsertAsync(station);

        return _mapper.ToDto(station);
    }

    [Authorize(KLCPermissions.Stations.Update)]
    public async Task<StationDto> UpdateAsync(Guid id, UpdateStationDto input)
    {
        var station = await _stationRepository.GetAsync(id);

        station.SetName(input.Name);
        station.SetAddress(input.Address);
        station.SetLocation(input.Latitude, input.Longitude);
        station.SetStationGroup(input.StationGroupId);
        station.SetTariffPlan(input.TariffPlanId);

        await _stationRepository.UpdateAsync(station);

        return _mapper.ToDto(station);
    }

    public async Task<StationDto> GetAsync(Guid id)
    {
        var query = await _stationRepository.WithDetailsAsync(s => s.Connectors);
        var station = await AsyncExecuter.FirstOrDefaultAsync(query.Where(s => s.Id == id));

        if (station == null)
        {
            throw new BusinessException("MOD_001_002")
                .WithData("id", id);
        }

        return _mapper.ToDto(station);
    }

    public async Task<PagedResultDto<StationListDto>> GetListAsync(GetStationListDto input)
    {
        var query = await _stationRepository.WithDetailsAsync(s => s.Connectors);

        // Apply filters
        if (input.Status.HasValue)
        {
            query = query.Where(s => s.Status == input.Status.Value);
        }

        if (input.StationGroupId.HasValue)
        {
            query = query.Where(s => s.StationGroupId == input.StationGroupId.Value);
        }

        if (input.IsEnabled.HasValue)
        {
            query = query.Where(s => s.IsEnabled == input.IsEnabled.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(search) ||
                s.StationCode.ToLower().Contains(search));
        }

        // Cursor-based pagination
        if (input.Cursor.HasValue)
        {
            query = query.Where(s => s.Id.CompareTo(input.Cursor.Value) > 0);
        }

        // Apply sorting
        query = input.SortBy?.ToLower() switch
        {
            "name" => input.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(s => s.Name)
                : query.OrderBy(s => s.Name),
            "stationcode" => input.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(s => s.StationCode)
                : query.OrderBy(s => s.StationCode),
            "status" => input.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(s => s.Status)
                : query.OrderBy(s => s.Status),
            _ => query.OrderBy(s => s.Id)
        };

        var totalCount = await AsyncExecuter.CountAsync(query);
        var stations = await AsyncExecuter.ToListAsync(query.Take(input.MaxResultCount));

        var dtos = stations.Select(s =>
        {
            var dto = _mapper.ToListDto(s);
            dto.ConnectorCount = s.Connectors.Count;
            return dto;
        }).ToList();

        return new PagedResultDto<StationListDto>(totalCount, dtos);
    }

    [Authorize(KLCPermissions.Stations.Decommission)]
    public async Task DecommissionAsync(Guid id)
    {
        var query = await _stationRepository.WithDetailsAsync(s => s.Connectors);
        var station = await AsyncExecuter.FirstOrDefaultAsync(query.Where(s => s.Id == id));

        if (station == null)
        {
            throw new BusinessException("MOD_001_002")
                .WithData("id", id);
        }

        // Check for active sessions would go here (MOD_001_003)
        // For now, just update status
        station.UpdateStatus(StationStatus.Decommissioned);
        station.Disable();

        await _stationRepository.UpdateAsync(station);
    }

    public async Task EnableAsync(Guid id)
    {
        var station = await _stationRepository.GetAsync(id);
        station.Enable();
        await _stationRepository.UpdateAsync(station);
    }

    public async Task DisableAsync(Guid id)
    {
        var station = await _stationRepository.GetAsync(id);
        station.Disable();
        await _stationRepository.UpdateAsync(station);
    }
}
