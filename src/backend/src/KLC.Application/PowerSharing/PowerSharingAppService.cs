using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Permissions;
using KLC.Stations;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Domain.Repositories;

namespace KLC.PowerSharing;

[Authorize(KLCPermissions.PowerSharing.Default)]
public class PowerSharingAppService : KLCAppService, IPowerSharingAppService
{
    private readonly IRepository<PowerSharingGroup, Guid> _groupRepository;
    private readonly IRepository<Connector, Guid> _connectorRepository;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<SiteLoadProfile, Guid> _loadProfileRepository;
    private readonly IPowerSharingService _powerSharingService;

    public PowerSharingAppService(
        IRepository<PowerSharingGroup, Guid> groupRepository,
        IRepository<Connector, Guid> connectorRepository,
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<SiteLoadProfile, Guid> loadProfileRepository,
        IPowerSharingService powerSharingService)
    {
        _groupRepository = groupRepository;
        _connectorRepository = connectorRepository;
        _stationRepository = stationRepository;
        _loadProfileRepository = loadProfileRepository;
        _powerSharingService = powerSharingService;
    }

    [Authorize(KLCPermissions.PowerSharing.Create)]
    public async Task<PowerSharingGroupDto> CreateAsync(CreatePowerSharingGroupDto input)
    {
        var group = new PowerSharingGroup(
            GuidGenerator.Create(),
            input.Name,
            input.MaxCapacityKw,
            input.Mode,
            input.DistributionStrategy,
            input.MinPowerPerConnectorKw,
            input.StationGroupId);

        await _groupRepository.InsertAsync(group);
        return await MapToDetailDtoAsync(group);
    }

    [Authorize(KLCPermissions.PowerSharing.Update)]
    public async Task<PowerSharingGroupDto> UpdateAsync(Guid id, UpdatePowerSharingGroupDto input)
    {
        var group = await _groupRepository.GetAsync(id);
        group.SetName(input.Name);
        group.SetMaxCapacity(input.MaxCapacityKw);
        group.SetDistributionStrategy(input.DistributionStrategy);
        group.SetMinPowerPerConnector(input.MinPowerPerConnectorKw);

        await _groupRepository.UpdateAsync(group);
        return await MapToDetailDtoAsync(group);
    }

    public async Task<PowerSharingGroupDto> GetAsync(Guid id)
    {
        var group = await _groupRepository.GetAsync(id);
        return await MapToDetailDtoAsync(group);
    }

    public async Task<List<PowerSharingGroupListDto>> GetListAsync(GetPowerSharingGroupListDto input)
    {
        var query = await _groupRepository.GetQueryableAsync();

        if (input.IsActive.HasValue)
            query = query.Where(g => g.IsActive == input.IsActive.Value);
        if (input.Mode.HasValue)
            query = query.Where(g => g.Mode == input.Mode.Value);
        if (!string.IsNullOrWhiteSpace(input.Search))
            query = query.Where(g => g.Name.ToLower().Contains(input.Search.ToLower()));

        if (!string.IsNullOrWhiteSpace(input.Cursor) && Guid.TryParse(input.Cursor, out var cursorId))
            query = query.Where(g => g.Id.CompareTo(cursorId) > 0);

        query = query.OrderBy(g => g.Id);

        var groups = await AsyncExecuter.ToListAsync(query.Take(input.PageSize));

        return groups.Select(g => new PowerSharingGroupListDto
        {
            Id = g.Id,
            Name = g.Name,
            MaxCapacityKw = g.MaxCapacityKw,
            Mode = g.Mode,
            DistributionStrategy = g.DistributionStrategy,
            IsActive = g.IsActive,
            MemberCount = g.Members.Count(m => !m.IsDeleted),
            TotalAllocatedKw = g.Members.Where(m => !m.IsDeleted).Sum(m => m.AllocatedPowerKw),
            CreationTime = g.CreationTime
        }).ToList();
    }

    [Authorize(KLCPermissions.PowerSharing.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await _groupRepository.DeleteAsync(id);
    }

    [Authorize(KLCPermissions.PowerSharing.Update)]
    public async Task ActivateAsync(Guid id)
    {
        var group = await _groupRepository.GetAsync(id);
        group.Activate();
        await _groupRepository.UpdateAsync(group);
    }

    [Authorize(KLCPermissions.PowerSharing.Update)]
    public async Task DeactivateAsync(Guid id)
    {
        var group = await _groupRepository.GetAsync(id);
        group.Deactivate();
        await _groupRepository.UpdateAsync(group);
    }

    [Authorize(KLCPermissions.PowerSharing.ManageMembers)]
    public async Task<PowerSharingMemberDto> AddMemberAsync(Guid groupId, AddMemberDto input)
    {
        var group = await _groupRepository.GetAsync(groupId);
        var member = group.AddMember(
            GuidGenerator.Create(),
            input.StationId,
            input.ConnectorId,
            input.Priority);

        await _groupRepository.UpdateAsync(group);
        return await MapMemberDtoAsync(member);
    }

    [Authorize(KLCPermissions.PowerSharing.ManageMembers)]
    public async Task RemoveMemberAsync(Guid groupId, Guid connectorId)
    {
        var group = await _groupRepository.GetAsync(groupId);
        group.RemoveMember(connectorId);
        await _groupRepository.UpdateAsync(group);
    }

    public async Task<List<PowerAllocationDto>> RecalculateAsync(Guid groupId)
    {
        var allocations = await _powerSharingService.RecalculateAllocationsAsync(groupId);
        return allocations.Select(a => new PowerAllocationDto
        {
            ConnectorId = a.ConnectorId,
            StationId = a.StationId,
            AllocatedPowerKw = a.AllocatedPowerKw,
            MaxPowerKw = a.MaxPowerKw
        }).ToList();
    }

    public async Task<List<SiteLoadProfileDto>> GetLoadProfilesAsync(Guid groupId, DateTime? from, DateTime? to)
    {
        var query = await _loadProfileRepository.GetQueryableAsync();
        query = query.Where(p => p.PowerSharingGroupId == groupId);

        if (from.HasValue)
            query = query.Where(p => p.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(p => p.Timestamp <= to.Value);

        query = query.OrderByDescending(p => p.Timestamp).Take(100);

        var profiles = await AsyncExecuter.ToListAsync(query);
        return profiles.Select(p => new SiteLoadProfileDto
        {
            Id = p.Id,
            PowerSharingGroupId = p.PowerSharingGroupId,
            Timestamp = p.Timestamp,
            TotalLoadKw = p.TotalLoadKw,
            AvailableCapacityKw = p.AvailableCapacityKw,
            ActiveSessionCount = p.ActiveSessionCount,
            TotalConnectorCount = p.TotalConnectorCount,
            PeakLoadKw = p.PeakLoadKw
        }).ToList();
    }

    private async Task<PowerSharingGroupDto> MapToDetailDtoAsync(PowerSharingGroup group)
    {
        var activeMembers = group.GetActiveMembers();
        var memberDtos = new List<PowerSharingMemberDto>();

        foreach (var member in activeMembers)
        {
            memberDtos.Add(await MapMemberDtoAsync(member));
        }

        return new PowerSharingGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            MaxCapacityKw = group.MaxCapacityKw,
            Mode = group.Mode,
            DistributionStrategy = group.DistributionStrategy,
            IsActive = group.IsActive,
            MinPowerPerConnectorKw = group.MinPowerPerConnectorKw,
            StationGroupId = group.StationGroupId,
            CreationTime = group.CreationTime,
            LastModificationTime = group.LastModificationTime,
            Members = memberDtos
        };
    }

    private async Task<PowerSharingMemberDto> MapMemberDtoAsync(PowerSharingGroupMember member)
    {
        var connector = await _connectorRepository.FindAsync(member.ConnectorId);
        var station = await _stationRepository.FindAsync(member.StationId);

        return new PowerSharingMemberDto
        {
            Id = member.Id,
            StationId = member.StationId,
            ConnectorId = member.ConnectorId,
            Priority = member.Priority,
            AllocatedPowerKw = member.AllocatedPowerKw,
            StationName = station?.Name,
            ConnectorNumber = connector?.ConnectorNumber ?? 0,
            ConnectorType = connector?.ConnectorType.ToString(),
            MaxPowerKw = connector?.MaxPowerKw ?? 0
        };
    }
}
