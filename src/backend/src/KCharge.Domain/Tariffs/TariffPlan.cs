using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KCharge.Tariffs;

/// <summary>
/// Represents a pricing plan for charging sessions.
/// Aggregate root for the Tariffs bounded context.
/// </summary>
public class TariffPlan : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Name of the tariff plan.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Description of the tariff plan.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Base rate per kWh in VND.
    /// </summary>
    public decimal BaseRatePerKwh { get; private set; }

    /// <summary>
    /// Tax rate percentage (e.g., 10 for 10% VAT).
    /// </summary>
    public decimal TaxRatePercent { get; private set; }

    /// <summary>
    /// When this tariff plan becomes effective.
    /// </summary>
    public DateTime EffectiveFrom { get; private set; }

    /// <summary>
    /// When this tariff plan expires (null = no expiry).
    /// </summary>
    public DateTime? EffectiveTo { get; private set; }

    /// <summary>
    /// Whether this tariff plan is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Whether this is the default tariff plan.
    /// </summary>
    public bool IsDefault { get; private set; }

    protected TariffPlan()
    {
        // Required by EF Core
    }

    public TariffPlan(
        Guid id,
        string name,
        decimal baseRatePerKwh,
        decimal taxRatePercent,
        DateTime effectiveFrom,
        DateTime? effectiveTo = null,
        string? description = null)
        : base(id)
    {
        SetName(name);
        SetBaseRate(baseRatePerKwh);
        SetTaxRate(taxRatePercent);
        SetEffectivePeriod(effectiveFrom, effectiveTo);
        Description = description;
        IsActive = true;
        IsDefault = false;
    }

    public void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 200);
    }

    public void SetDescription(string? description)
    {
        Description = description;
    }

    public void SetBaseRate(decimal baseRatePerKwh)
    {
        if (baseRatePerKwh < 0)
            throw new ArgumentException("Base rate cannot be negative", nameof(baseRatePerKwh));
        BaseRatePerKwh = baseRatePerKwh;
    }

    public void SetTaxRate(decimal taxRatePercent)
    {
        if (taxRatePercent < 0 || taxRatePercent > 100)
            throw new ArgumentException("Tax rate must be between 0 and 100", nameof(taxRatePercent));
        TaxRatePercent = taxRatePercent;
    }

    public void SetEffectivePeriod(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        if (effectiveTo.HasValue && effectiveTo < effectiveFrom)
            throw new ArgumentException("EffectiveTo must be after EffectiveFrom");
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public void SetAsDefault()
    {
        IsDefault = true;
    }

    public void RemoveDefault()
    {
        IsDefault = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
        IsDefault = false;
    }

    /// <summary>
    /// Calculate total rate including tax.
    /// </summary>
    public decimal GetTotalRatePerKwh()
    {
        return BaseRatePerKwh * (1 + TaxRatePercent / 100);
    }

    /// <summary>
    /// Check if tariff plan is currently effective.
    /// </summary>
    public bool IsCurrentlyEffective()
    {
        var now = DateTime.UtcNow;
        return IsActive &&
               EffectiveFrom <= now &&
               (!EffectiveTo.HasValue || EffectiveTo > now);
    }
}
