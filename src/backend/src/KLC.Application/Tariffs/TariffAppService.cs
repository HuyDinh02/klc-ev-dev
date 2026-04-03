using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace KLC.Tariffs;

[Authorize(KLCPermissions.Tariffs.Default)]
public class TariffAppService : KLCAppService, ITariffAppService
{
    private readonly IRepository<TariffPlan, Guid> _tariffRepository;

    public TariffAppService(IRepository<TariffPlan, Guid> tariffRepository)
    {
        _tariffRepository = tariffRepository;
    }

    [Authorize(KLCPermissions.Tariffs.Create)]
    public async Task<TariffPlanDto> CreateAsync(CreateTariffPlanDto input)
    {
        var tariff = new TariffPlan(
            GuidGenerator.Create(),
            input.Name,
            input.BaseRatePerKwh,
            input.TaxRatePercent,
            input.EffectiveFrom,
            input.EffectiveTo,
            input.Description
        );

        await _tariffRepository.InsertAsync(tariff);
        return MapToDto(tariff);
    }

    [Authorize(KLCPermissions.Tariffs.Update)]
    public async Task<TariffPlanDto> UpdateAsync(Guid id, UpdateTariffPlanDto input)
    {
        var tariff = await _tariffRepository.GetAsync(id);

        tariff.SetName(input.Name);
        tariff.SetDescription(input.Description);
        tariff.SetBaseRate(input.BaseRatePerKwh);
        tariff.SetTaxRate(input.TaxRatePercent);
        tariff.SetEffectivePeriod(input.EffectiveFrom, input.EffectiveTo);

        await _tariffRepository.UpdateAsync(tariff);
        return MapToDto(tariff);
    }

    public async Task<TariffPlanDto> GetAsync(Guid id)
    {
        var tariff = await _tariffRepository.GetAsync(id);
        return MapToDto(tariff);
    }

    public async Task<PagedResultDto<TariffPlanListDto>> GetListAsync(GetTariffPlanListDto input)
    {
        var query = await _tariffRepository.GetQueryableAsync();

        if (input.IsActive.HasValue)
        {
            query = query.Where(t => t.IsActive == input.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(search));
        }

        query = query.OrderByDescending(t => t.IsDefault).ThenBy(t => t.Name);

        var totalCount = await AsyncExecuter.CountAsync(query);
        var items = await AsyncExecuter.ToListAsync(query.Take(input.MaxResultCount));

        var dtos = items.Select(t => new TariffPlanListDto
        {
            Id = t.Id,
            Name = t.Name,
            BaseRatePerKwh = t.BaseRatePerKwh,
            TaxRatePercent = t.TaxRatePercent,
            TotalRatePerKwh = Math.Round(t.BaseRatePerKwh * (1 + t.TaxRatePercent / 100), 0),
            IsActive = t.IsActive,
            IsDefault = t.IsDefault,
            EffectiveFrom = t.EffectiveFrom
        }).ToList();

        return new PagedResultDto<TariffPlanListDto>(totalCount, dtos);
    }

    [Authorize(KLCPermissions.Tariffs.Activate)]
    public async Task ActivateAsync(Guid id)
    {
        var tariff = await _tariffRepository.GetAsync(id);
        tariff.Activate();
        await _tariffRepository.UpdateAsync(tariff);
    }

    [Authorize(KLCPermissions.Tariffs.Deactivate)]
    public async Task DeactivateAsync(Guid id)
    {
        var tariff = await _tariffRepository.GetAsync(id);
        tariff.Deactivate();
        await _tariffRepository.UpdateAsync(tariff);
    }

    [Authorize(KLCPermissions.Tariffs.Update)]
    public async Task SetAsDefaultAsync(Guid id)
    {
        var tariffs = await _tariffRepository.GetListAsync();

        foreach (var t in tariffs)
        {
            if (t.Id == id)
            {
                t.SetAsDefault();
            }
            else if (t.IsDefault)
            {
                t.RemoveDefault();
            }
        }

        await _tariffRepository.UpdateManyAsync(tariffs);
    }

    public async Task<decimal> CalculateCostAsync(Guid tariffPlanId, decimal energyKwh)
    {
        var tariff = await _tariffRepository.GetAsync(tariffPlanId);

        var baseCost = energyKwh * tariff.BaseRatePerKwh;
        var tax = baseCost * (tariff.TaxRatePercent / 100);
        return Math.Round(baseCost + tax, 0);
    }

    private static TariffPlanDto MapToDto(TariffPlan tariff)
    {
        return new TariffPlanDto
        {
            Id = tariff.Id,
            Name = tariff.Name,
            Description = tariff.Description,
            BaseRatePerKwh = tariff.BaseRatePerKwh,
            TaxRatePercent = tariff.TaxRatePercent,
            EffectiveFrom = tariff.EffectiveFrom,
            EffectiveTo = tariff.EffectiveTo,
            IsActive = tariff.IsActive,
            IsDefault = tariff.IsDefault,
            TotalRatePerKwh = tariff.GetTotalRatePerKwh(),
            CreationTime = tariff.CreationTime,
            CreatorId = tariff.CreatorId,
            LastModificationTime = tariff.LastModificationTime,
            LastModifierId = tariff.LastModifierId
        };
    }
}
