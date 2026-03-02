using System;
using System.Threading.Tasks;
using KLC.Faults;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace KLC.Controllers.Faults;

[ApiController]
[Route("api/v1/faults")]
public class FaultController : KLCController
{
    private readonly IFaultAppService _faultAppService;

    public FaultController(IFaultAppService faultAppService)
    {
        _faultAppService = faultAppService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FaultDto>> GetAsync(Guid id)
    {
        var result = await _faultAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<FaultListDto>>> GetListAsync([FromQuery] GetFaultListDto input)
    {
        var result = await _faultAppService.GetListAsync(input);
        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<FaultDto>> UpdateStatusAsync(Guid id, [FromBody] UpdateFaultStatusDto input)
    {
        var result = await _faultAppService.UpdateStatusAsync(id, input);
        return Ok(result);
    }
}

[ApiController]
[Route("api/v1/stations/{stationId:guid}/faults")]
public class StationFaultController : KLCController
{
    private readonly IFaultAppService _faultAppService;

    public StationFaultController(IFaultAppService faultAppService)
    {
        _faultAppService = faultAppService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<FaultListDto>>> GetByStationAsync(Guid stationId, [FromQuery] GetFaultListDto input)
    {
        var result = await _faultAppService.GetByStationAsync(stationId, input);
        return Ok(result);
    }
}
