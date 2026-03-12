using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Operators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace KLC.Controllers.Operators;

/// <summary>
/// B2B Operator API — authenticated via X-API-Key header (middleware).
/// Allows external operators to query their stations and sessions.
/// </summary>
[ApiController]
[Route("api/v1/operator")]
public class OperatorApiController : KLCController
{
    private readonly IOperatorApiAppService _operatorApiAppService;

    public OperatorApiController(IOperatorApiAppService operatorApiAppService)
    {
        _operatorApiAppService = operatorApiAppService;
    }

    /// <summary>
    /// List operator's stations.
    /// </summary>
    [HttpGet("stations")]
    public async Task<ActionResult<List<OperatorStationListItemDto>>> GetStationsAsync(
        [FromQuery] string? cursor, [FromQuery] int pageSize = 20)
    {
        var operatorId = GetOperatorId();
        var result = await _operatorApiAppService.GetStationsAsync(operatorId, cursor, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get station detail with connectors.
    /// </summary>
    [HttpGet("stations/{id:guid}")]
    public async Task<ActionResult<OperatorStationDetailDto>> GetStationAsync(Guid id)
    {
        var operatorId = GetOperatorId();
        var result = await _operatorApiAppService.GetStationAsync(operatorId, id);
        return Ok(result);
    }

    /// <summary>
    /// Get session history for operator's stations.
    /// </summary>
    [HttpGet("sessions")]
    public async Task<ActionResult<List<OperatorSessionDto>>> GetSessionsAsync(
        [FromQuery] string? cursor,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? stationId = null)
    {
        var operatorId = GetOperatorId();
        var result = await _operatorApiAppService.GetSessionsAsync(
            operatorId, cursor, pageSize, fromDate, toDate, stationId);
        return Ok(result);
    }

    /// <summary>
    /// Get active sessions for operator's stations.
    /// </summary>
    [HttpGet("sessions/active")]
    public async Task<ActionResult<List<OperatorSessionDto>>> GetActiveSessionsAsync()
    {
        var operatorId = GetOperatorId();
        var result = await _operatorApiAppService.GetActiveSessionsAsync(operatorId);
        return Ok(result);
    }

    /// <summary>
    /// Get usage analytics for the operator's stations (last 30 days).
    /// </summary>
    [HttpGet("analytics/summary")]
    public async Task<ActionResult<OperatorAnalyticsSummaryDto>> GetAnalyticsSummaryAsync()
    {
        var operatorId = GetOperatorId();
        var result = await _operatorApiAppService.GetAnalyticsSummaryAsync(operatorId);
        return Ok(result);
    }

    private Guid GetOperatorId()
    {
        if (HttpContext.Items.TryGetValue("OperatorId", out var operatorIdObj) && operatorIdObj is Guid operatorId)
            return operatorId;

        throw new BusinessException(KLCDomainErrorCodes.Operators.InvalidApiKey);
    }
}
