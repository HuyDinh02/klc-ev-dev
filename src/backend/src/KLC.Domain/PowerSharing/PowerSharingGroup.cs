using System;
using System.Collections.Generic;
using System.Linq;
using KLC.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.PowerSharing;

/// <summary>
/// Represents a group of chargers sharing a common power pool.
/// Supports LINK (single site) and LOOP (multi-site) modes.
/// </summary>
public class PowerSharingGroup : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Display name of the power sharing group.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Total available power capacity for the group in kW.
    /// This is the site circuit breaker or grid connection limit.
    /// </summary>
    public decimal MaxCapacityKw { get; private set; }

    /// <summary>
    /// Power sharing mode (LINK or LOOP).
    /// </summary>
    public PowerSharingMode Mode { get; private set; }

    /// <summary>
    /// Strategy for distributing power among active sessions.
    /// </summary>
    public PowerDistributionStrategy DistributionStrategy { get; private set; }

    /// <summary>
    /// Whether this power sharing group is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Minimum power per connector in kW. Ensures each active session
    /// gets at least this amount regardless of distribution strategy.
    /// </summary>
    public decimal MinPowerPerConnectorKw { get; private set; }

    /// <summary>
    /// Optional reference to a station group for organizational purposes.
    /// </summary>
    public Guid? StationGroupId { get; private set; }

    /// <summary>
    /// Members (connectors) in this power sharing group.
    /// </summary>
    public ICollection<PowerSharingGroupMember> Members { get; private set; } = new List<PowerSharingGroupMember>();

    protected PowerSharingGroup()
    {
        // Required by EF Core
    }

    public PowerSharingGroup(
        Guid id,
        string name,
        decimal maxCapacityKw,
        PowerSharingMode mode,
        PowerDistributionStrategy distributionStrategy = PowerDistributionStrategy.Average,
        decimal minPowerPerConnectorKw = 1.4m,
        Guid? stationGroupId = null)
        : base(id)
    {
        SetName(name);
        SetMaxCapacity(maxCapacityKw);
        Mode = mode;
        DistributionStrategy = distributionStrategy;
        SetMinPowerPerConnector(minPowerPerConnectorKw);
        StationGroupId = stationGroupId;
        IsActive = true;
    }

    public void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 200);
    }

    public void SetMaxCapacity(decimal maxCapacityKw)
    {
        if (maxCapacityKw <= 0)
            throw new BusinessException(KLCDomainErrorCodes.PowerSharing.InvalidCapacity);
        MaxCapacityKw = maxCapacityKw;
    }

    public void SetMinPowerPerConnector(decimal minPowerKw)
    {
        if (minPowerKw < 0)
            throw new BusinessException(KLCDomainErrorCodes.PowerSharing.InvalidMinPower);
        MinPowerPerConnectorKw = minPowerKw;
    }

    public void SetDistributionStrategy(PowerDistributionStrategy strategy)
    {
        DistributionStrategy = strategy;
    }

    public void SetMode(PowerSharingMode mode)
    {
        Mode = mode;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public PowerSharingGroupMember AddMember(
        Guid memberId,
        Guid stationId,
        Guid connectorId,
        int priority = 0)
    {
        if (Members.Any(m => m.ConnectorId == connectorId && !m.IsDeleted))
            throw new BusinessException(KLCDomainErrorCodes.PowerSharing.ConnectorAlreadyInGroup);

        if (Mode == PowerSharingMode.Link && Members.Count(m => !m.IsDeleted) >= 10)
            throw new BusinessException(KLCDomainErrorCodes.PowerSharing.MaxMembersExceeded);

        var member = new PowerSharingGroupMember(memberId, Id, stationId, connectorId, priority);
        Members.Add(member);
        return member;
    }

    public void RemoveMember(Guid connectorId)
    {
        var member = Members.FirstOrDefault(m => m.ConnectorId == connectorId && !m.IsDeleted);
        if (member == null)
            throw new BusinessException(KLCDomainErrorCodes.PowerSharing.ConnectorNotInGroup);

        member.MarkAsDeleted();
    }

    /// <summary>
    /// Gets currently active (non-deleted) members.
    /// </summary>
    public IReadOnlyList<PowerSharingGroupMember> GetActiveMembers()
    {
        return Members.Where(m => !m.IsDeleted).ToList();
    }
}
