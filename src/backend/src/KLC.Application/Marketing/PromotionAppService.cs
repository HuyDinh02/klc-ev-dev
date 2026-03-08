using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KLC.Files;
using KLC.MobileUsers;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace KLC.Marketing;

[Authorize(KLCPermissions.Promotions.Default)]
public class PromotionAppService : KLCAppService, IPromotionAppService
{
    private readonly IRepository<Promotion, Guid> _promotionRepository;
    private readonly IFileUploadService _fileUploadService;

    public PromotionAppService(
        IRepository<Promotion, Guid> promotionRepository,
        IFileUploadService fileUploadService)
    {
        _promotionRepository = promotionRepository;
        _fileUploadService = fileUploadService;
    }

    public async Task<CursorPagedResultDto<PromotionListDto>> GetListAsync(GetPromotionListDto input)
    {
        var pageSize = input.PageSize;
        if (pageSize <= 0 || pageSize > 50) pageSize = 20;

        var query = await _promotionRepository.GetQueryableAsync();
        query = query.Where(p => !p.IsDeleted);
        query = query.OrderByDescending(p => p.CreationTime);

        var promotions = await AsyncExecuter.ToListAsync(query.Take(pageSize + 1));
        var hasMore = promotions.Count > pageSize;
        var items = hasMore ? promotions.Take(pageSize).ToList() : promotions;

        var dtos = items.Select(p => new PromotionListDto
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            ImageUrl = p.ImageUrl,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Type = p.Type,
            IsActive = p.IsActive,
            IsCurrentlyActive = p.IsCurrentlyActive(),
            CreatedAt = p.CreationTime
        }).ToList();

        return new CursorPagedResultDto<PromotionListDto>
        {
            Data = dtos,
            Pagination = new CursorPaginationDto
            {
                HasMore = hasMore,
                PageSize = pageSize
            }
        };
    }

    public async Task<PromotionDetailDto> GetAsync(Guid id)
    {
        var promo = await _promotionRepository.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (promo == null)
            throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);

        return new PromotionDetailDto
        {
            Id = promo.Id,
            Title = promo.Title,
            Description = promo.Description,
            ImageUrl = promo.ImageUrl,
            StartDate = promo.StartDate,
            EndDate = promo.EndDate,
            Type = promo.Type,
            IsActive = promo.IsActive,
            IsCurrentlyActive = promo.IsCurrentlyActive(),
            CreatedAt = promo.CreationTime
        };
    }

    [Authorize(KLCPermissions.Promotions.Create)]
    public async Task<CreatePromotionResultDto> CreateAsync(CreatePromotionDto input)
    {
        var promo = new Promotion(
            GuidGenerator.Create(),
            input.Title,
            input.StartDate,
            input.EndDate,
            input.Type,
            input.Description,
            input.ImageUrl);

        await _promotionRepository.InsertAsync(promo);

        return new CreatePromotionResultDto { Id = promo.Id, Title = promo.Title };
    }

    [Authorize(KLCPermissions.Promotions.Update)]
    public async Task UpdateAsync(Guid id, UpdatePromotionDto input)
    {
        var promo = await _promotionRepository.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (promo == null)
            throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);

        promo.Update(input.Title, input.Description, input.ImageUrl,
            input.StartDate, input.EndDate, input.Type, input.IsActive);
        await _promotionRepository.UpdateAsync(promo);
    }

    [Authorize(KLCPermissions.Promotions.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var promo = await _promotionRepository.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (promo == null)
            throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);

        await _promotionRepository.DeleteAsync(promo);
    }

    [Authorize(KLCPermissions.Promotions.Create)]
    public async Task<ImageUploadResultDto> UploadImageAsync(Stream stream, string fileName)
    {
        var result = await _fileUploadService.UploadAsync(stream, fileName, "promotions");
        return new ImageUploadResultDto
        {
            Url = result.Url,
            FileSize = result.FileSize
        };
    }
}
