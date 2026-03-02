using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace KLC.Tariffs;

public interface ITariffAppService : IApplicationService
{
    Task<TariffPlanDto> CreateAsync(CreateTariffPlanDto input);
    Task<TariffPlanDto> UpdateAsync(Guid id, UpdateTariffPlanDto input);
    Task<TariffPlanDto> GetAsync(Guid id);
    Task<PagedResultDto<TariffPlanListDto>> GetListAsync(GetTariffPlanListDto input);
    Task ActivateAsync(Guid id);
    Task DeactivateAsync(Guid id);
    Task SetAsDefaultAsync(Guid id);
    Task<decimal> CalculateCostAsync(Guid tariffPlanId, decimal energyKwh);
}
