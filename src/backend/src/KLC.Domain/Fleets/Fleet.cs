using System;
using System.Collections.Generic;
using System.Linq;
using KLC.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Fleets;

/// <summary>
/// Represents a fleet of vehicles managed by an operator (e.g., taxi company, delivery fleet).
/// Aggregate root for the Fleets bounded context.
/// </summary>
public class Fleet : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Display name of the fleet.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Reference to the admin user managing this fleet.
    /// </summary>
    public Guid OperatorUserId { get; private set; }

    /// <summary>
    /// Optional description of the fleet.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Maximum monthly budget in VND for charging across the fleet.
    /// </summary>
    public decimal MaxMonthlyBudgetVnd { get; private set; }

    /// <summary>
    /// Amount spent on charging in the current month (VND).
    /// </summary>
    public decimal CurrentMonthSpentVnd { get; private set; }

    /// <summary>
    /// Charging policy enforced on this fleet's vehicles.
    /// </summary>
    public ChargingPolicyType ChargingPolicy { get; private set; }

    /// <summary>
    /// Whether this fleet is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Percentage of budget utilization at which alerts are triggered.
    /// </summary>
    public int BudgetAlertThresholdPercent { get; private set; }

    /// <summary>
    /// Collection of vehicles belonging to this fleet.
    /// </summary>
    public ICollection<FleetVehicle> Vehicles { get; private set; } = new List<FleetVehicle>();

    protected Fleet()
    {
        // Required by EF Core
    }

    public Fleet(
        Guid id,
        string name,
        Guid operatorUserId,
        string? description = null,
        decimal maxMonthlyBudgetVnd = 0,
        ChargingPolicyType chargingPolicy = ChargingPolicyType.AnytimeAnywhere,
        int budgetAlertThresholdPercent = 80)
        : base(id)
    {
        SetName(name);
        OperatorUserId = operatorUserId;
        Description = description;
        SetBudget(maxMonthlyBudgetVnd, budgetAlertThresholdPercent);
        ChargingPolicy = chargingPolicy;
        IsActive = true;
        CurrentMonthSpentVnd = 0;
    }

    public void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 200);
    }

    public void SetDescription(string? description)
    {
        Description = description;
    }

    public void SetBudget(decimal maxMonthlyBudgetVnd, int budgetAlertThresholdPercent)
    {
        if (maxMonthlyBudgetVnd < 0)
            throw new BusinessException(KLCDomainErrorCodes.Fleet.InvalidBudget);

        if (budgetAlertThresholdPercent < 0 || budgetAlertThresholdPercent > 100)
            throw new BusinessException(KLCDomainErrorCodes.Fleet.InvalidBudget);

        MaxMonthlyBudgetVnd = maxMonthlyBudgetVnd;
        BudgetAlertThresholdPercent = budgetAlertThresholdPercent;
    }

    public void SetChargingPolicy(ChargingPolicyType chargingPolicy)
    {
        ChargingPolicy = chargingPolicy;
    }

    public FleetVehicle AddVehicle(Guid vehicleId, Guid? driverUserId = null, decimal? dailyLimitKwh = null)
    {
        if (Vehicles.Any(v => v.VehicleId == vehicleId && !v.IsDeleted))
            throw new BusinessException(KLCDomainErrorCodes.Fleet.VehicleAlreadyInFleet);

        var fleetVehicle = new FleetVehicle(
            Guid.NewGuid(),
            Id,
            vehicleId,
            driverUserId,
            dailyLimitKwh);

        Vehicles.Add(fleetVehicle);
        return fleetVehicle;
    }

    public void RemoveVehicle(Guid vehicleId)
    {
        var vehicle = Vehicles.FirstOrDefault(v => v.VehicleId == vehicleId && !v.IsDeleted);
        if (vehicle == null)
            throw new BusinessException(KLCDomainErrorCodes.Fleet.VehicleNotInFleet);

        vehicle.SoftDelete();
    }

    public void RecordSpending(decimal amountVnd)
    {
        CurrentMonthSpentVnd += amountVnd;
    }

    public void ResetMonthlySpending()
    {
        CurrentMonthSpentVnd = 0;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public bool IsBudgetExceeded()
    {
        return MaxMonthlyBudgetVnd > 0 && CurrentMonthSpentVnd >= MaxMonthlyBudgetVnd;
    }

    public decimal GetBudgetUtilizationPercent()
    {
        if (MaxMonthlyBudgetVnd <= 0) return 0;
        return Math.Round(CurrentMonthSpentVnd / MaxMonthlyBudgetVnd * 100, 2);
    }
}
