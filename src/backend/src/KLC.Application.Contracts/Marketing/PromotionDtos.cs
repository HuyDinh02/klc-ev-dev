using System;
using System.ComponentModel.DataAnnotations;
using KLC.Enums;

namespace KLC.Marketing;

public class PromotionListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PromotionType Type { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrentlyActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PromotionDetailDto : PromotionListDto
{
}

public class CreatePromotionDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public PromotionType Type { get; set; }
}

public class UpdatePromotionDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public PromotionType? Type { get; set; }
    public bool? IsActive { get; set; }
}

public class GetPromotionListDto
{
    public Guid? Cursor { get; set; }

    [Range(1, 50)]
    public int PageSize { get; set; } = 20;
}

public class CreatePromotionResultDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
}
