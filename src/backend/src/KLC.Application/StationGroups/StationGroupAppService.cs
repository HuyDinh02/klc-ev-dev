using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.Permissions;
using KLC.Stations;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace KLC.StationGroups;

[Authorize(KLCPermissions.StationGroups.Default)]
public class StationGroupAppService : KLCAppService, IStationGroupAppService
{
    private readonly IRepository<StationGroup, Guid> _groupRepository;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;

    public StationGroupAppService(
        IRepository<StationGroup, Guid> groupRepository,
        IRepository<ChargingStation, Guid> stationRepository)
    {
        _groupRepository = groupRepository;
        _stationRepository = stationRepository;
    }

    [Authorize(KLCPermissions.StationGroups.Create)]
    public async Task<StationGroupDto> CreateAsync(CreateStationGroupDto input)
    {
        var group = new StationGroup(
            GuidGenerator.Create(),
            input.Name,
            input.Description,
            input.Region
        );

        await _groupRepository.InsertAsync(group);

        return MapToDto(group, 0);
    }

    public async Task<PagedResultDto<StationGroupListDto>> GetListAsync(GetStationGroupListDto input)
    {
        var query = await _groupRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.ToLower();
            query = query.Where(g => g.Name.ToLower().Contains(search) ||
                                     (g.Description != null && g.Description.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(input.Region))
        {
            query = query.Where(g => g.Region == input.Region);
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(g => g.IsActive == input.IsActive.Value);
        }

        if (input.Cursor.HasValue)
        {
            query = query.Where(g => g.Id.CompareTo(input.Cursor.Value) > 0);
        }

        query = query.OrderBy(g => g.Name);

        var totalCount = await AsyncExecuter.CountAsync(query);
        var groups = await AsyncExecuter.ToListAsync(query.Take(input.MaxResultCount));

        // Get station counts
        var groupIds = groups.Select(g => g.Id).ToList();
        var stations = await _stationRepository.GetListAsync(s => s.StationGroupId.HasValue && groupIds.Contains(s.StationGroupId.Value));
        var stationCounts = stations.GroupBy(s => s.StationGroupId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var dtos = groups.Select(g => new StationGroupListDto
        {
            Id = g.Id,
            Name = g.Name,
            Region = g.Region,
            IsActive = g.IsActive,
            StationCount = stationCounts.TryGetValue(g.Id, out var count) ? count : 0
        }).ToList();

        return new PagedResultDto<StationGroupListDto>(totalCount, dtos);
    }

    public async Task<StationGroupDetailDto> GetAsync(Guid id)
    {
        var group = await _groupRepository.GetAsync(id);
        var stations = await _stationRepository.GetListAsync(s => s.StationGroupId == id);

        return new StationGroupDetailDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            Region = group.Region,
            IsActive = group.IsActive,
            StationCount = stations.Count,
            CreationTime = group.CreationTime,
            CreatorId = group.CreatorId,
            Stations = stations.Select(s => new GroupStationDto
            {
                StationId = s.Id,
                Name = s.Name,
                Address = s.Address,
                Status = s.Status.ToString()
            }).ToList()
        };
    }

    [Authorize(KLCPermissions.StationGroups.Update)]
    public async Task<StationGroupDto> UpdateAsync(Guid id, UpdateStationGroupDto input)
    {
        var group = await _groupRepository.GetAsync(id);

        group.SetName(input.Name);
        group.SetDescription(input.Description);
        group.SetRegion(input.Region);

        await _groupRepository.UpdateAsync(group);

        var stationCount = await _stationRepository.CountAsync(s => s.StationGroupId == id);

        return MapToDto(group, (int)stationCount);
    }

    [Authorize(KLCPermissions.StationGroups.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        // Unassign all stations from this group
        var stations = await _stationRepository.GetListAsync(s => s.StationGroupId == id);
        foreach (var station in stations)
        {
            station.SetStationGroup(null);
        }
        if (stations.Any())
        {
            await _stationRepository.UpdateManyAsync(stations);
        }

        await _groupRepository.DeleteAsync(id);
    }

    [Authorize(KLCPermissions.StationGroups.Assign)]
    public async Task AssignStationAsync(Guid groupId, AssignStationDto input)
    {
        var group = await _groupRepository.GetAsync(groupId);
        var station = await _stationRepository.GetAsync(input.StationId);

        // Check if station is already in a group
        if (station.StationGroupId.HasValue && station.StationGroupId.Value != groupId)
        {
            throw new BusinessException(KLCDomainErrorCodes.StationGroup.StationAlreadyAssigned)
                .WithData("stationName", station.Name);
        }

        station.SetStationGroup(groupId);
        await _stationRepository.UpdateAsync(station);
    }

    [Authorize(KLCPermissions.StationGroups.Assign)]
    public async Task UnassignStationAsync(Guid groupId, Guid stationId)
    {
        var station = await _stationRepository.GetAsync(stationId);

        if (station.StationGroupId != groupId)
        {
            throw new BusinessException(KLCDomainErrorCodes.StationGroup.StationNotInGroup);
        }

        station.SetStationGroup(null);
        await _stationRepository.UpdateAsync(station);
    }

    private static StationGroupDto MapToDto(StationGroup group, int stationCount)
    {
        return new StationGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            Region = group.Region,
            IsActive = group.IsActive,
            StationCount = stationCount,
            CreationTime = group.CreationTime,
            CreatorId = group.CreatorId
        };
    }
}
