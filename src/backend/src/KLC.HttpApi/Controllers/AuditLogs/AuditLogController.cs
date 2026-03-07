using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.AuditLogs;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace KLC.Controllers.AuditLogs;

[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(KLCPermissions.AuditLogs.Default)]
public class AuditLogController : KLCController
{
    private readonly IAuditLogAppService _auditLogAppService;

    public AuditLogController(IAuditLogAppService auditLogAppService)
    {
        _auditLogAppService = auditLogAppService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AuditLogListDto>>> GetListAsync([FromQuery] GetAuditLogListDto input)
    {
        var result = await _auditLogAppService.GetListAsync(input);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuditLogDto>> GetAsync(Guid id)
    {
        var result = await _auditLogAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpGet("entity-changes")]
    public async Task<ActionResult<PagedResultDto<EntityChangeDto>>> GetEntityChangesAsync([FromQuery] GetEntityChangesDto input)
    {
        var result = await _auditLogAppService.GetEntityChangesAsync(input);
        return Ok(result);
    }

    [HttpGet("entity-changes/{entityChangeId:guid}/property-changes")]
    public async Task<ActionResult<List<EntityPropertyChangeDto>>> GetPropertyChangesAsync(Guid entityChangeId)
    {
        var result = await _auditLogAppService.GetPropertyChangesAsync(entityChangeId);
        return Ok(result);
    }

    [HttpGet("export")]
    public async Task<ActionResult> ExportToCsvAsync([FromQuery] GetAuditLogListDto input)
    {
        var csvBytes = await _auditLogAppService.ExportToCsvAsync(input);
        return File(csvBytes, "text/csv", $"audit-logs-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }
}
