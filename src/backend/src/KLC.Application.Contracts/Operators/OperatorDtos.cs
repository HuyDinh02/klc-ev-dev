using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace KLC.Operators;

public class OperatorDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? WebhookUrl { get; set; }
    public bool IsActive { get; set; }
    public int RateLimitPerMinute { get; set; }
    public string? Description { get; set; }
    public int StationCount { get; set; }
    public DateTime CreationTime { get; set; }
}

public class OperatorDetailDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? WebhookUrl { get; set; }
    public bool IsActive { get; set; }
    public int RateLimitPerMinute { get; set; }
    public string? Description { get; set; }
    public int StationCount { get; set; }
    public DateTime CreationTime { get; set; }
    public List<OperatorStationDto> AllowedStations { get; set; } = [];
}

public class OperatorStationDto
{
    public Guid StationId { get; set; }
    public string StationCode { get; set; } = string.Empty;
    public string StationName { get; set; } = string.Empty;
}

public class CreateOperatorDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(1, 100000)]
    public int RateLimitPerMinute { get; set; } = 1000;
}

public class UpdateOperatorDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;

    [StringLength(500)]
    public string? WebhookUrl { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(1, 100000)]
    public int RateLimitPerMinute { get; set; } = 1000;
}

public class OperatorApiKeyDto
{
    public string ApiKey { get; set; } = string.Empty;
}

public class CreateOperatorResultDto
{
    public OperatorDetailDto Operator { get; set; } = null!;
    public string ApiKey { get; set; } = string.Empty;
}

public class GetOperatorListDto
{
    public string? Cursor { get; set; }
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}
