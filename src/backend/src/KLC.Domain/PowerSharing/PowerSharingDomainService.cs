using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Sessions;
using KLC.Stations;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;

namespace KLC.PowerSharing;

public class PowerSharingDomainService : DomainService, IPowerSharingService
{
    private readonly IRepository<PowerSharingGroup, Guid> _groupRepository;
    private readonly IRepository<Connector, Guid> _connectorRepository;
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<SiteLoadProfile, Guid> _loadProfileRepository;

    public PowerSharingDomainService(
        IRepository<PowerSharingGroup, Guid> groupRepository,
        IRepository<Connector, Guid> connectorRepository,
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<SiteLoadProfile, Guid> loadProfileRepository)
    {
        _groupRepository = groupRepository;
        _connectorRepository = connectorRepository;
        _sessionRepository = sessionRepository;
        _loadProfileRepository = loadProfileRepository;
    }

    public async Task<IReadOnlyList<PowerAllocation>> RecalculateAllocationsAsync(Guid powerSharingGroupId)
    {
        var group = await _groupRepository.GetAsync(powerSharingGroupId);
        if (!group.IsActive)
            return [];

        var activeMembers = group.GetActiveMembers();
        if (activeMembers.Count == 0)
            return [];

        // Get connector details for max power
        var connectorIds = activeMembers.Select(m => m.ConnectorId).ToList();
        var connectors = await _connectorRepository.GetListAsync(c => connectorIds.Contains(c.Id));

        // Find which connectors have active charging sessions
        // Session tracks StationId + ConnectorNumber, so we need to match via connector lookup
        var connectorLookup = connectors.ToDictionary(c => c.Id, c => new { c.StationId, c.ConnectorNumber });
        var stationIds = activeMembers.Select(m => m.StationId).Distinct().ToList();
        var activeSessions = await _sessionRepository.GetListAsync(
            s => stationIds.Contains(s.StationId) &&
                 (s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Starting));

        var activeSessionConnectorIds = new HashSet<Guid>();
        foreach (var member in activeMembers)
        {
            if (connectorLookup.TryGetValue(member.ConnectorId, out var info))
            {
                if (activeSessions.Any(s => s.StationId == info.StationId && s.ConnectorNumber == info.ConnectorNumber))
                    activeSessionConnectorIds.Add(member.ConnectorId);
            }
        }

        var chargingMembers = activeMembers
            .Where(m => activeSessionConnectorIds.Contains(m.ConnectorId))
            .ToList();

        if (chargingMembers.Count == 0)
            return activeMembers.Select(m => new PowerAllocation(
                m.ConnectorId, m.StationId, 0, GetMaxPower(connectors, m.ConnectorId))).ToList();

        var allocations = group.DistributionStrategy switch
        {
            PowerDistributionStrategy.Average => CalculateAverage(group, chargingMembers, connectors),
            PowerDistributionStrategy.Proportional => CalculateProportional(group, chargingMembers, connectors),
            PowerDistributionStrategy.Dynamic => CalculateDynamic(group, chargingMembers, connectors),
            _ => CalculateAverage(group, chargingMembers, connectors)
        };

        // Update allocated power on members
        foreach (var allocation in allocations)
        {
            var member = activeMembers.FirstOrDefault(m => m.ConnectorId == allocation.ConnectorId);
            member?.UpdateAllocatedPower(allocation.AllocatedPowerKw);
        }

        // Add zero allocations for non-charging members
        var chargingConnectorIds = chargingMembers.Select(m => m.ConnectorId).ToHashSet();
        var idleAllocations = activeMembers
            .Where(m => !chargingConnectorIds.Contains(m.ConnectorId))
            .Select(m => new PowerAllocation(m.ConnectorId, m.StationId, 0, GetMaxPower(connectors, m.ConnectorId)));

        return allocations.Concat(idleAllocations).ToList();
    }

    public async Task<decimal?> GetAllocatedPowerAsync(Guid connectorId)
    {
        var groups = await _groupRepository.GetListAsync(g => g.IsActive);
        foreach (var group in groups)
        {
            var member = group.Members.FirstOrDefault(m => m.ConnectorId == connectorId && !m.IsDeleted);
            if (member != null)
                return member.AllocatedPowerKw;
        }
        return null;
    }

    public async Task RecordLoadProfileAsync(Guid powerSharingGroupId)
    {
        var group = await _groupRepository.GetAsync(powerSharingGroupId);
        var activeMembers = group.GetActiveMembers();

        var totalLoad = activeMembers.Sum(m => m.AllocatedPowerKw);
        var peakLoad = activeMembers.Count > 0 ? activeMembers.Max(m => m.AllocatedPowerKw) : 0;

        var connectorIds = activeMembers.Select(m => m.ConnectorId).ToList();
        var connectors = await _connectorRepository.GetListAsync(c => connectorIds.Contains(c.Id));
        var stationIds = activeMembers.Select(m => m.StationId).Distinct().ToList();
        var activeSessions = await _sessionRepository.GetListAsync(
            s => stationIds.Contains(s.StationId) &&
                 (s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Starting));
        var activeSessionCount = 0;
        foreach (var member in activeMembers)
        {
            var connector = connectors.FirstOrDefault(c => c.Id == member.ConnectorId);
            if (connector != null && activeSessions.Any(s => s.StationId == connector.StationId && s.ConnectorNumber == connector.ConnectorNumber))
                activeSessionCount++;
        }

        var profile = new SiteLoadProfile(
            GuidGenerator.Create(),
            powerSharingGroupId,
            DateTime.UtcNow,
            totalLoad,
            group.MaxCapacityKw - totalLoad,
            activeSessionCount,
            activeMembers.Count,
            peakLoad);

        await _loadProfileRepository.InsertAsync(profile);

        Logger.LogInformation(
            "Recorded load profile for group {GroupId}: {TotalLoad}kW / {MaxCapacity}kW, {ActiveSessions} active sessions",
            powerSharingGroupId, totalLoad, group.MaxCapacityKw, activeSessionCount);
    }

    private List<PowerAllocation> CalculateAverage(
        PowerSharingGroup group,
        List<PowerSharingGroupMember> chargingMembers,
        List<Connector> connectors)
    {
        var equalShare = group.MaxCapacityKw / chargingMembers.Count;

        return chargingMembers.Select(m =>
        {
            var maxPower = GetMaxPower(connectors, m.ConnectorId);
            var allocated = Math.Min(equalShare, maxPower);
            allocated = Math.Max(allocated, group.MinPowerPerConnectorKw);
            return new PowerAllocation(m.ConnectorId, m.StationId, allocated, maxPower);
        }).ToList();
    }

    private List<PowerAllocation> CalculateProportional(
        PowerSharingGroup group,
        List<PowerSharingGroupMember> chargingMembers,
        List<Connector> connectors)
    {
        var totalMaxPower = chargingMembers.Sum(m => GetMaxPower(connectors, m.ConnectorId));
        if (totalMaxPower == 0)
            return CalculateAverage(group, chargingMembers, connectors);

        return chargingMembers.Select(m =>
        {
            var maxPower = GetMaxPower(connectors, m.ConnectorId);
            var ratio = maxPower / totalMaxPower;
            var allocated = group.MaxCapacityKw * ratio;
            allocated = Math.Min(allocated, maxPower);
            allocated = Math.Max(allocated, group.MinPowerPerConnectorKw);
            return new PowerAllocation(m.ConnectorId, m.StationId, allocated, maxPower);
        }).ToList();
    }

    private List<PowerAllocation> CalculateDynamic(
        PowerSharingGroup group,
        List<PowerSharingGroupMember> chargingMembers,
        List<Connector> connectors)
    {
        // Dynamic: prioritize by member priority, then distribute remaining
        var sorted = chargingMembers.OrderByDescending(m => m.Priority).ToList();
        var remaining = group.MaxCapacityKw;
        var allocations = new List<PowerAllocation>();

        foreach (var member in sorted)
        {
            var maxPower = GetMaxPower(connectors, member.ConnectorId);
            var allocated = Math.Min(remaining, maxPower);
            allocated = Math.Max(allocated, Math.Min(group.MinPowerPerConnectorKw, remaining));
            allocations.Add(new PowerAllocation(member.ConnectorId, member.StationId, allocated, maxPower));
            remaining -= allocated;
            if (remaining <= 0) remaining = 0;
        }

        return allocations;
    }

    private static decimal GetMaxPower(List<Connector> connectors, Guid connectorId)
    {
        return connectors.FirstOrDefault(c => c.Id == connectorId)?.MaxPowerKw ?? 0;
    }
}
