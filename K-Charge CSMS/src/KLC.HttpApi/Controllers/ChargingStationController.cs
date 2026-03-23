using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;
using KLC.ChargingStations;

namespace KLC.HttpApi.Controllers;

[Area("app")]
[ControllerName("ChargingStations")]
[Route("api/app/charging-stations")]
public class ChargingStationController : AbpControllerBase
{
    private readonly IChargingStationAppService _appService;

    public ChargingStationController(IChargingStationAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<PagedResultDto<ChargingStationDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        return await _appService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public async Task<ChargingStationDto> GetAsync(Guid id)
    {
        return await _appService.GetAsync(id);
    }

    [HttpPost]
    public async Task<ChargingStationDto> CreateAsync(CreateChargingStationDto input)
    {
        return await _appService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    public async Task<ChargingStationDto> UpdateAsync(Guid id, CreateChargingStationDto input)
    {
        return await _appService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    public async Task DeleteAsync(Guid id)
    {
        await _appService.DeleteAsync(id);
    }
}
