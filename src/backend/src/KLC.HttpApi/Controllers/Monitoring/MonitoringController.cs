using System;
using System.Threading.Tasks;
using KLC.Monitoring;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace KLC.Controllers.Monitoring;

[ApiController]
[Route("api/v1/monitoring")]
public class MonitoringController : KLCController
{
    private readonly IMonitoringAppService _monitoringAppService;

    public MonitoringController(IMonitoringAppService monitoringAppService)
    {
        _monitoringAppService = monitoringAppService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboardAsync()
    {
        var result = await _monitoringAppService.GetDashboardAsync();
        return Ok(result);
    }

    [HttpGet("stations/{stationId:guid}/status-history")]
    public async Task<ActionResult<PagedResultDto<StatusChangeLogDto>>> GetStatusHistoryAsync(
        Guid stationId,
        [FromQuery] GetStatusHistoryDto input)
    {
        var result = await _monitoringAppService.GetStatusHistoryAsync(stationId, input);
        return Ok(result);
    }

    [HttpGet("stations/{stationId:guid}/energy-summary")]
    public async Task<ActionResult<EnergySummaryDto>> GetStationEnergySummaryAsync(
        Guid stationId,
        [FromQuery] GetEnergySummaryDto input)
    {
        var result = await _monitoringAppService.GetStationEnergySummaryAsync(stationId, input);
        return Ok(result);
    }

    [HttpGet("connectors/{connectorId:guid}/energy-summary")]
    public async Task<ActionResult<EnergySummaryDto>> GetConnectorEnergySummaryAsync(
        Guid connectorId,
        [FromQuery] GetEnergySummaryDto input)
    {
        var result = await _monitoringAppService.GetConnectorEnergySummaryAsync(connectorId, input);
        return Ok(result);
    }
}
