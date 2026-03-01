using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace KCharge.Tariffs;

public class TariffPlanDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BaseRatePerKwh { get; set; }
    public decimal TaxRatePercent { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public decimal TotalRatePerKwh { get; set; }
}

public class TariffPlanListDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal BaseRatePerKwh { get; set; }
    public decimal TaxRatePercent { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime EffectiveFrom { get; set; }
}

public class CreateTariffPlanDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Range(0, 1000000)]
    public decimal BaseRatePerKwh { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal TaxRatePercent { get; set; }

    [Required]
    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }
}

public class UpdateTariffPlanDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Range(0, 1000000)]
    public decimal BaseRatePerKwh { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal TaxRatePercent { get; set; }

    [Required]
    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }
}

public class GetTariffPlanListDto : LimitedResultRequestDto
{
    public bool? IsActive { get; set; }
    public string? Search { get; set; }
}
