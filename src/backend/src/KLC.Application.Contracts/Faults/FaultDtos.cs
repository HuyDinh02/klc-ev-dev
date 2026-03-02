using System;
using System.ComponentModel.DataAnnotations;
using KLC.Enums;
using Volo.Abp.Application.Dtos;

namespace KLC.Faults;

public class FaultDto : FullAuditedEntityDto<Guid>
{
    public Guid StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int? ConnectorNumber { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string? ErrorInfo { get; set; }
    public string? VendorErrorCode { get; set; }
    public int Priority { get; set; }
    public FaultStatus Status { get; set; }
    public DateTime DetectedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}

public class FaultListDto : EntityDto<Guid>
{
    public string StationName { get; set; } = string.Empty;
    public int? ConnectorNumber { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public FaultStatus Status { get; set; }
    public DateTime DetectedAt { get; set; }
}

public class UpdateFaultStatusDto
{
    [Required]
    public FaultStatus Status { get; set; }

    [StringLength(1000)]
    public string? ResolutionNotes { get; set; }
}

public class GetFaultListDto : LimitedResultRequestDto
{
    public Guid? StationId { get; set; }
    public FaultStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
    public Guid? Cursor { get; set; }
}
