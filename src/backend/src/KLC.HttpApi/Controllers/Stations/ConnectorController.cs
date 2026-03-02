using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Stations;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Controllers.Stations;

[ApiController]
[Route("api/v1")]
public class ConnectorController : KLCController
{
    private readonly IConnectorAppService _connectorAppService;

    public ConnectorController(IConnectorAppService connectorAppService)
    {
        _connectorAppService = connectorAppService;
    }

    /// <summary>
    /// Creates a new connector for a station.
    /// </summary>
    [HttpPost("stations/{stationId:guid}/connectors")]
    public async Task<ActionResult<ConnectorDto>> CreateAsync(Guid stationId, [FromBody] CreateConnectorDto input)
    {
        var result = await _connectorAppService.CreateAsync(stationId, input);
        return CreatedAtAction(nameof(GetAsync), new { id = result.Id }, result);
    }

    /// <summary>
    /// Gets all connectors for a station.
    /// </summary>
    [HttpGet("stations/{stationId:guid}/connectors")]
    public async Task<ActionResult<List<ConnectorDto>>> GetListByStationAsync(Guid stationId)
    {
        var result = await _connectorAppService.GetListByStationAsync(stationId);
        return Ok(result);
    }

    /// <summary>
    /// Gets a connector by ID.
    /// </summary>
    [HttpGet("connectors/{id:guid}")]
    public async Task<ActionResult<ConnectorDto>> GetAsync(Guid id)
    {
        var result = await _connectorAppService.GetAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Updates a connector.
    /// </summary>
    [HttpPut("connectors/{id:guid}")]
    public async Task<ActionResult<ConnectorDto>> UpdateAsync(Guid id, [FromBody] UpdateConnectorDto input)
    {
        var result = await _connectorAppService.UpdateAsync(id, input);
        return Ok(result);
    }

    /// <summary>
    /// Enables a connector.
    /// </summary>
    [HttpPost("connectors/{id:guid}/enable")]
    public async Task<ActionResult> EnableAsync(Guid id)
    {
        await _connectorAppService.EnableAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Disables a connector.
    /// </summary>
    [HttpPost("connectors/{id:guid}/disable")]
    public async Task<ActionResult> DisableAsync(Guid id)
    {
        await _connectorAppService.DisableAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Deletes a connector.
    /// </summary>
    [HttpDelete("connectors/{id:guid}")]
    public async Task<ActionResult> DeleteAsync(Guid id)
    {
        await _connectorAppService.DeleteAsync(id);
        return NoContent();
    }
}
