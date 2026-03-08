using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
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
    private readonly IRepository<Connector, Guid> _connectorRepository;

    public StationGroupAppService(
        IRepository<StationGroup, Guid> groupRepository,
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<Connector, Guid> connectorRepository)
    {
        _groupRepository = groupRepository;
        _stationRepository = stationRepository;
        _connectorRepository = connectorRepository;
    }

    [Authorize(KLCPermissions.StationGroups.Create)]
    public async Task<StationGroupDto> CreateAsync(CreateStationGroupDto input)
    {
        if (input.ParentGroupId.HasValue)
        {
            var parentExists = await _groupRepository.AnyAsync(g => g.Id == input.ParentGroupId.Value);
            if (!parentExists)
                throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);
        }

        var group = new StationGroup(
            GuidGenerator.Create(),
            input.Name,
            input.Description,
            input.Region,
            input.GroupType,
            input.ParentGroupId
        );

        await _groupRepository.InsertAsync(group);

        return MapToDto(group, 0, 0, null);
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

        if (input.GroupType.HasValue)
        {
            query = query.Where(g => g.GroupType == input.GroupType.Value);
        }

        if (input.TopLevelOnly == true)
        {
            query = query.Where(g => g.ParentGroupId == null);
        }
        else if (input.ParentGroupId.HasValue)
        {
            query = query.Where(g => g.ParentGroupId == input.ParentGroupId.Value);
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
        var stations = await _stationRepository.GetListAsync(
            s => s.StationGroupId.HasValue && groupIds.Contains(s.StationGroupId.Value));
        var stationCounts = stations.GroupBy(s => s.StationGroupId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // Get child group counts
        var allGroups = await _groupRepository.GetListAsync(
            g => g.ParentGroupId.HasValue && groupIds.Contains(g.ParentGroupId.Value));
        var childCounts = allGroups.GroupBy(g => g.ParentGroupId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // Get parent group names
        var parentIds = groups.Where(g => g.ParentGroupId.HasValue)
            .Select(g => g.ParentGroupId!.Value).Distinct().ToList();
        var parentGroups = parentIds.Any()
            ? await _groupRepository.GetListAsync(g => parentIds.Contains(g.Id))
            : new List<StationGroup>();
        var parentNames = parentGroups.ToDictionary(g => g.Id, g => g.Name);

        var dtos = groups.Select(g => new StationGroupListDto
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            Region = g.Region,
            GroupType = g.GroupType,
            ParentGroupId = g.ParentGroupId,
            ParentGroupName = g.ParentGroupId.HasValue && parentNames.TryGetValue(g.ParentGroupId.Value, out var pn) ? pn : null,
            IsActive = g.IsActive,
            StationCount = stationCounts.TryGetValue(g.Id, out var count) ? count : 0,
            ChildGroupCount = childCounts.TryGetValue(g.Id, out var cc) ? cc : 0
        }).ToList();

        return new PagedResultDto<StationGroupListDto>(totalCount, dtos);
    }

    public async Task<StationGroupDetailDto> GetAsync(Guid id)
    {
        var group = await _groupRepository.GetAsync(id);
        var stations = await _stationRepository.GetListAsync(s => s.StationGroupId == id);
        var stationIds = stations.Select(s => s.Id).ToList();

        // Get connectors for stats
        var connectors = stationIds.Any()
            ? await _connectorRepository.GetListAsync(c => stationIds.Contains(c.StationId))
            : new List<Connector>();

        // Get child groups
        var childGroups = await _groupRepository.GetListAsync(g => g.ParentGroupId == id);
        var childIds = childGroups.Select(g => g.Id).ToList();
        var childStationCounts = new Dictionary<Guid, int>();
        if (childIds.Any())
        {
            var childStations = await _stationRepository.GetListAsync(
                s => s.StationGroupId.HasValue && childIds.Contains(s.StationGroupId.Value));
            childStationCounts = childStations.GroupBy(s => s.StationGroupId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        // Get parent name
        string? parentName = null;
        if (group.ParentGroupId.HasValue)
        {
            var parent = await _groupRepository.FindAsync(group.ParentGroupId.Value);
            parentName = parent?.Name;
        }

        // Build connector stats grouped by station
        var connectorsByStation = connectors.GroupBy(c => c.StationId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var stationDtos = stations.Select(s =>
        {
            var stationConnectors = connectorsByStation.TryGetValue(s.Id, out var sc) ? sc : new List<Connector>();
            return new GroupStationDto
            {
                StationId = s.Id,
                Name = s.Name,
                Address = s.Address,
                Status = s.Status.ToString(),
                ConnectorCount = stationConnectors.Count,
                AvailableConnectors = stationConnectors.Count(c => c.Status == ConnectorStatus.Available)
            };
        }).ToList();

        return new StationGroupDetailDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            Region = group.Region,
            GroupType = group.GroupType,
            ParentGroupId = group.ParentGroupId,
            ParentGroupName = parentName,
            IsActive = group.IsActive,
            StationCount = stations.Count,
            ChildGroupCount = childGroups.Count,
            CreationTime = group.CreationTime,
            CreatorId = group.CreatorId,
            Stations = stationDtos,
            ChildGroups = childGroups.Select(cg => new StationGroupListDto
            {
                Id = cg.Id,
                Name = cg.Name,
                Description = cg.Description,
                Region = cg.Region,
                GroupType = cg.GroupType,
                ParentGroupId = cg.ParentGroupId,
                ParentGroupName = group.Name,
                IsActive = cg.IsActive,
                StationCount = childStationCounts.TryGetValue(cg.Id, out var cc) ? cc : 0
            }).ToList(),
            Stats = new StationGroupStatsDto
            {
                TotalStations = stations.Count,
                TotalConnectors = connectors.Count,
                AvailableConnectors = connectors.Count(c => c.Status == ConnectorStatus.Available),
                OccupiedConnectors = connectors.Count(c => c.Status == ConnectorStatus.Charging),
                FaultedConnectors = connectors.Count(c => c.Status == ConnectorStatus.Faulted),
                OfflineStations = stations.Count(s => s.Status == StationStatus.Offline),
                TotalCapacityKw = (double)connectors.Sum(c => c.MaxPowerKw)
            }
        };
    }

    [Authorize(KLCPermissions.StationGroups.Update)]
    public async Task<StationGroupDto> UpdateAsync(Guid id, UpdateStationGroupDto input)
    {
        var group = await _groupRepository.GetAsync(id);

        group.SetName(input.Name);
        group.SetDescription(input.Description);
        group.SetRegion(input.Region);

        if (input.GroupType.HasValue)
            group.SetGroupType(input.GroupType.Value);

        if (input.ParentGroupId.HasValue && input.ParentGroupId.Value != id)
        {
            var parentExists = await _groupRepository.AnyAsync(g => g.Id == input.ParentGroupId.Value);
            if (!parentExists)
                throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);
            group.SetParentGroup(input.ParentGroupId);
        }
        else if (input.ParentGroupId == null)
        {
            group.SetParentGroup(null);
        }

        if (input.IsActive.HasValue)
        {
            if (input.IsActive.Value) group.Activate();
            else group.Deactivate();
        }

        await _groupRepository.UpdateAsync(group);

        var stationCount = await _stationRepository.CountAsync(s => s.StationGroupId == id);
        var childCount = await _groupRepository.CountAsync(g => g.ParentGroupId == id);

        string? parentName = null;
        if (group.ParentGroupId.HasValue)
        {
            var parent = await _groupRepository.FindAsync(group.ParentGroupId.Value);
            parentName = parent?.Name;
        }

        return MapToDto(group, (int)stationCount, (int)childCount, parentName);
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

        // Move child groups to parent (or make top-level)
        var childGroups = await _groupRepository.GetListAsync(g => g.ParentGroupId == id);
        var group = await _groupRepository.GetAsync(id);
        foreach (var child in childGroups)
        {
            child.SetParentGroup(group.ParentGroupId);
        }
        if (childGroups.Any())
        {
            await _groupRepository.UpdateManyAsync(childGroups);
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

    private static StationGroupDto MapToDto(StationGroup group, int stationCount, int childGroupCount, string? parentName)
    {
        return new StationGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            Region = group.Region,
            GroupType = group.GroupType,
            ParentGroupId = group.ParentGroupId,
            ParentGroupName = parentName,
            IsActive = group.IsActive,
            StationCount = stationCount,
            ChildGroupCount = childGroupCount,
            CreationTime = group.CreationTime,
            CreatorId = group.CreatorId
        };
    }
}
