using System;
using System.Threading.Tasks;
using KLC.Tariffs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace KLC.Controllers.Tariffs;

[ApiController]
[Route("api/v1/tariffs")]
public class TariffController : KLCController
{
    private readonly ITariffAppService _tariffAppService;

    public TariffController(ITariffAppService tariffAppService)
    {
        _tariffAppService = tariffAppService;
    }

    [HttpPost]
    public async Task<ActionResult<TariffPlanDto>> CreateAsync([FromBody] CreateTariffPlanDto input)
    {
        var result = await _tariffAppService.CreateAsync(input);
        return CreatedAtAction(nameof(GetAsync), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<TariffPlanDto>> GetAsync(Guid id)
    {
        var result = await _tariffAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResultDto<TariffPlanListDto>>> GetListAsync([FromQuery] GetTariffPlanListDto input)
    {
        var result = await _tariffAppService.GetListAsync(input);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TariffPlanDto>> UpdateAsync(Guid id, [FromBody] UpdateTariffPlanDto input)
    {
        var result = await _tariffAppService.UpdateAsync(id, input);
        return Ok(result);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult> ActivateAsync(Guid id)
    {
        await _tariffAppService.ActivateAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult> DeactivateAsync(Guid id)
    {
        await _tariffAppService.DeactivateAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/set-default")]
    public async Task<ActionResult> SetAsDefaultAsync(Guid id)
    {
        await _tariffAppService.SetAsDefaultAsync(id);
        return NoContent();
    }

    [HttpGet("{id:guid}/calculate")]
    public async Task<ActionResult<decimal>> CalculateCostAsync(Guid id, [FromQuery] decimal energyKwh)
    {
        var cost = await _tariffAppService.CalculateCostAsync(id, energyKwh);
        return Ok(cost);
    }
}
