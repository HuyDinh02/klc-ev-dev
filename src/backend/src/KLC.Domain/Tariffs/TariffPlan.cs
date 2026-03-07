using System;
using System.Collections.Generic;
using System.Linq;
using KLC.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Tariffs;

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

    /// <summary>
    /// Tariff pricing model (Flat or TimeOfUse).
    /// </summary>
    public TariffType TariffType { get; private set; } = TariffType.Flat;

    /// <summary>
    /// Off-peak rate per kWh in VND (23:00–06:00). Only used when TariffType = TimeOfUse.
    /// </summary>
    public decimal? OffPeakRatePerKwh { get; private set; }

    /// <summary>
    /// Normal rate per kWh in VND (06:00–17:00, 21:00–23:00). Only used when TariffType = TimeOfUse.
    /// </summary>
    public decimal? NormalRatePerKwh { get; private set; }

    /// <summary>
    /// Peak rate per kWh in VND (17:00–21:00). Only used when TariffType = TimeOfUse.
    /// </summary>
    public decimal? PeakRatePerKwh { get; private set; }

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
            throw new BusinessException(KLCDomainErrorCodes.Tariff.InvalidBaseRate);
        BaseRatePerKwh = baseRatePerKwh;
    }

    public void SetTaxRate(decimal taxRatePercent)
    {
        if (taxRatePercent < 0 || taxRatePercent > 100)
            throw new BusinessException(KLCDomainErrorCodes.Tariff.InvalidTaxRate);
        TaxRatePercent = taxRatePercent;
    }

    public void SetEffectivePeriod(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        if (effectiveTo.HasValue && effectiveTo < effectiveFrom)
            throw new BusinessException(KLCDomainErrorCodes.Tariff.InvalidEffectivePeriod);
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

    public void SetTouRates(decimal offPeakRate, decimal normalRate, decimal peakRate)
    {
        if (offPeakRate < 0 || normalRate < 0 || peakRate < 0)
            throw new BusinessException(KLCDomainErrorCodes.Tariff.InvalidBaseRate);
        TariffType = TariffType.TimeOfUse;
        OffPeakRatePerKwh = offPeakRate;
        NormalRatePerKwh = normalRate;
        PeakRatePerKwh = peakRate;
    }

    public void SetFlatRate()
    {
        TariffType = TariffType.Flat;
        OffPeakRatePerKwh = null;
        NormalRatePerKwh = null;
        PeakRatePerKwh = null;
    }

    /// <summary>
    /// Calculate total rate including tax. For Flat tariffs only.
    /// </summary>
    public decimal GetTotalRatePerKwh()
    {
        return BaseRatePerKwh * (1 + TaxRatePercent / 100);
    }

    /// <summary>
    /// Get rate per kWh (including tax) for a specific time of day.
    /// For Flat tariffs, returns the flat rate regardless of time.
    /// For TOU tariffs, returns the tier rate based on Vietnam EVN schedule.
    /// </summary>
    public decimal GetRateForTime(DateTime utcTime)
    {
        if (TariffType == TariffType.Flat)
            return GetTotalRatePerKwh();

        // Convert UTC to Vietnam time (UTC+7)
        var vnTime = utcTime.AddHours(7);
        var hour = vnTime.Hour;

        decimal baseRate;
        if (hour >= 23 || hour < 6)
            baseRate = OffPeakRatePerKwh ?? BaseRatePerKwh; // Off-peak: 23:00–06:00
        else if (hour >= 17 && hour < 21)
            baseRate = PeakRatePerKwh ?? BaseRatePerKwh;    // Peak: 17:00–21:00
        else
            baseRate = NormalRatePerKwh ?? BaseRatePerKwh;   // Normal: 06:00–17:00, 21:00–23:00

        return baseRate * (1 + TaxRatePercent / 100);
    }

    /// <summary>
    /// Get the TOU tier name for a given time.
    /// </summary>
    public static string GetTierName(DateTime utcTime)
    {
        var vnTime = utcTime.AddHours(7);
        var hour = vnTime.Hour;

        if (hour >= 23 || hour < 6) return "OffPeak";
        if (hour >= 17 && hour < 21) return "Peak";
        return "Normal";
    }

    /// <summary>
    /// Calculate TOU cost breakdown for a session using meter value timestamps.
    /// Returns total cost and per-tier breakdown.
    /// </summary>
    public TouCostBreakdown CalculateTouCost(IReadOnlyList<(DateTime Timestamp, decimal EnergyKwh)> meterValues,
        int? meterStartWh, int? meterStopWh, DateTime? startTime, DateTime? endTime)
    {
        if (TariffType == TariffType.Flat || meterValues.Count < 2)
        {
            // Fall back to flat calculation
            decimal totalKwh = 0;
            if (meterStartWh.HasValue && meterStopWh.HasValue)
                totalKwh = Math.Round((meterStopWh.Value - meterStartWh.Value) / 1000m, 3);
            var flatCost = Math.Round(totalKwh * GetTotalRatePerKwh(), 0);
            return new TouCostBreakdown(flatCost, 0, 0, 0, 0, 0, 0);
        }

        // Walk through consecutive meter value pairs and apportion energy to tiers
        decimal offPeakKwh = 0, normalKwh = 0, peakKwh = 0;
        var sorted = meterValues.OrderBy(mv => mv.Timestamp).ToList();

        for (int i = 1; i < sorted.Count; i++)
        {
            var deltaKwh = sorted[i].EnergyKwh - sorted[i - 1].EnergyKwh;
            if (deltaKwh <= 0) continue;

            // Use midpoint time to determine tier
            var midpoint = sorted[i - 1].Timestamp.AddTicks(
                (sorted[i].Timestamp - sorted[i - 1].Timestamp).Ticks / 2);

            var tier = GetTierName(midpoint);
            switch (tier)
            {
                case "OffPeak": offPeakKwh += deltaKwh; break;
                case "Peak": peakKwh += deltaKwh; break;
                default: normalKwh += deltaKwh; break;
            }
        }

        var taxMultiplier = 1 + TaxRatePercent / 100;
        var offPeakCost = Math.Round(offPeakKwh * (OffPeakRatePerKwh ?? BaseRatePerKwh) * taxMultiplier, 0);
        var normalCost = Math.Round(normalKwh * (NormalRatePerKwh ?? BaseRatePerKwh) * taxMultiplier, 0);
        var peakCost = Math.Round(peakKwh * (PeakRatePerKwh ?? BaseRatePerKwh) * taxMultiplier, 0);

        return new TouCostBreakdown(
            offPeakCost + normalCost + peakCost,
            offPeakKwh, normalKwh, peakKwh,
            offPeakCost, normalCost, peakCost);
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

/// <summary>
/// Time-of-Use cost breakdown result.
/// </summary>
public record TouCostBreakdown(
    decimal TotalCost,
    decimal OffPeakKwh,
    decimal NormalKwh,
    decimal PeakKwh,
    decimal OffPeakCost,
    decimal NormalCost,
    decimal PeakCost);
