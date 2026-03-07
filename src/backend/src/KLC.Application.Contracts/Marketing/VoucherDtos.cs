using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KLC.Enums;

namespace KLC.Marketing;

public class VoucherListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public VoucherType Type { get; set; }
    public decimal Value { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int TotalQuantity { get; set; }
    public int UsedQuantity { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VoucherDetailDto : VoucherListDto
{
}

public class CreateVoucherDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public VoucherType Type { get; set; }

    [Required]
    public decimal Value { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int TotalQuantity { get; set; }

    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public string? Description { get; set; }
}

public class UpdateVoucherDto
{
    public string? Description { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? TotalQuantity { get; set; }
    public bool? IsActive { get; set; }
}

public class GetVoucherListDto
{
    public bool? IsActive { get; set; }
    public Guid? Cursor { get; set; }

    [Range(1, 50)]
    public int PageSize { get; set; } = 20;
}

public class VoucherUsageDto
{
    public Guid UserId { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime ClaimedAt { get; set; }
}

public class VoucherUsageResultDto
{
    public int TotalQuantity { get; set; }
    public int UsedQuantity { get; set; }
    public List<VoucherUsageDto> Usages { get; set; } = new();
}

public class CreateVoucherResultDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
}
