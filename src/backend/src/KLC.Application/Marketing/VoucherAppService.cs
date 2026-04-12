using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using KLC.MobileUsers;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace KLC.Marketing;

[Authorize(KLCPermissions.Vouchers.Default)]
public class VoucherAppService : KLCAppService, IVoucherAppService
{
    private readonly IRepository<Voucher, Guid> _voucherRepository;
    private readonly IRepository<UserVoucher, Guid> _userVoucherRepository;

    public VoucherAppService(
        IRepository<Voucher, Guid> voucherRepository,
        IRepository<UserVoucher, Guid> userVoucherRepository)
    {
        _voucherRepository = voucherRepository;
        _userVoucherRepository = userVoucherRepository;
    }

    public async Task<CursorPagedResultDto<VoucherListDto>> GetListAsync(GetVoucherListDto input)
    {
        var pageSize = input.PageSize;
        if (pageSize <= 0 || pageSize > 50) pageSize = 20;

        var query = await _voucherRepository.GetQueryableAsync();
        query = query.Where(v => !v.IsDeleted);

        if (input.IsActive.HasValue)
            query = query.Where(v => v.IsActive == input.IsActive.Value);

        if (input.Cursor.HasValue)
        {
            var cursorVoucher = await _voucherRepository.FirstOrDefaultAsync(v => v.Id == input.Cursor.Value);
            if (cursorVoucher != null)
                query = query.Where(v => v.CreationTime < cursorVoucher.CreationTime);
        }

        query = query.OrderByDescending(v => v.CreationTime);

        var vouchers = await AsyncExecuter.ToListAsync(query.Take(pageSize + 1));
        var hasMore = vouchers.Count > pageSize;
        var items = hasMore ? vouchers.Take(pageSize).ToList() : vouchers;

        var dtos = items.Select(v => new VoucherListDto
        {
            Id = v.Id,
            Code = v.Code,
            Type = v.Type,
            Value = v.Value,
            ExpiryDate = v.ExpiryDate,
            TotalQuantity = v.TotalQuantity,
            UsedQuantity = v.UsedQuantity,
            IsActive = v.IsActive,
            Description = v.Description,
            MinOrderAmount = v.MinOrderAmount,
            MaxDiscountAmount = v.MaxDiscountAmount,
            PromotionId = v.PromotionId,
            CreatedAt = v.CreationTime
        }).ToList();

        return new CursorPagedResultDto<VoucherListDto>
        {
            Data = dtos,
            Pagination = new CursorPaginationDto
            {
                HasMore = hasMore,
                PageSize = pageSize
            }
        };
    }

    public async Task<VoucherDetailDto> GetAsync(Guid id)
    {
        var voucher = await _voucherRepository.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
        if (voucher == null)
            throw new BusinessException(KLCDomainErrorCodes.Voucher.NotFound);

        return new VoucherDetailDto
        {
            Id = voucher.Id,
            Code = voucher.Code,
            Type = voucher.Type,
            Value = voucher.Value,
            ExpiryDate = voucher.ExpiryDate,
            TotalQuantity = voucher.TotalQuantity,
            UsedQuantity = voucher.UsedQuantity,
            IsActive = voucher.IsActive,
            Description = voucher.Description,
            MinOrderAmount = voucher.MinOrderAmount,
            MaxDiscountAmount = voucher.MaxDiscountAmount,
            PromotionId = voucher.PromotionId,
            CreatedAt = voucher.CreationTime
        };
    }

    [Authorize(KLCPermissions.Vouchers.Create)]
    public async Task<CreateVoucherResultDto> CreateAsync(CreateVoucherDto input)
    {
        var exists = await _voucherRepository.AnyAsync(v => v.Code == input.Code && !v.IsDeleted);
        if (exists)
            throw new BusinessException(KLCDomainErrorCodes.Voucher.DuplicateCode);

        var voucher = new Voucher(
            GuidGenerator.Create(),
            input.Code,
            input.Type,
            input.Value,
            input.ExpiryDate,
            input.TotalQuantity,
            input.MinOrderAmount,
            input.MaxDiscountAmount,
            input.Description,
            input.PromotionId);

        await _voucherRepository.InsertAsync(voucher);

        return new CreateVoucherResultDto { Id = voucher.Id, Code = voucher.Code };
    }

    [Authorize(KLCPermissions.Vouchers.Update)]
    public async Task UpdateAsync(Guid id, UpdateVoucherDto input)
    {
        var voucher = await _voucherRepository.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
        if (voucher == null)
            throw new BusinessException(KLCDomainErrorCodes.Voucher.NotFound);

        voucher.Update(input.Description, input.ExpiryDate, input.TotalQuantity, input.IsActive);
        await _voucherRepository.UpdateAsync(voucher);
    }

    [Authorize(KLCPermissions.Vouchers.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var voucher = await _voucherRepository.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
        if (voucher == null)
            throw new BusinessException(KLCDomainErrorCodes.Voucher.NotFound);

        voucher.Deactivate();
        await _voucherRepository.UpdateAsync(voucher);
    }

    public async Task<VoucherUsageResultDto> GetUsageAsync(Guid id)
    {
        var voucher = await _voucherRepository.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
        if (voucher == null)
            throw new BusinessException(KLCDomainErrorCodes.Voucher.NotFound);

        var query = await _userVoucherRepository.GetQueryableAsync();
        var usages = await AsyncExecuter.ToListAsync(
            query.Where(uv => uv.VoucherId == id)
                .OrderByDescending(uv => uv.CreationTime)
                .Take(50));

        return new VoucherUsageResultDto
        {
            TotalQuantity = voucher.TotalQuantity,
            UsedQuantity = voucher.UsedQuantity,
            Usages = usages.Select(uv => new VoucherUsageDto
            {
                UserId = uv.UserId,
                IsUsed = uv.IsUsed,
                UsedAt = uv.UsedAt,
                ClaimedAt = uv.CreationTime
            }).ToList()
        };
    }

    [Authorize(KLCPermissions.Vouchers.Create)]
    public async Task<BulkCreateVoucherResultDto> BulkCreateAsync(BulkCreateVoucherDto input)
    {
        var vouchers = new List<Voucher>();
        var results = new List<CreateVoucherResultDto>();

        for (var i = 0; i < input.Count; i++)
        {
            var code = GenerateUniqueCode();

            // Ensure uniqueness
            while (await _voucherRepository.AnyAsync(v => v.Code == code && !v.IsDeleted))
            {
                code = GenerateUniqueCode();
            }

            var voucher = new Voucher(
                GuidGenerator.Create(),
                code,
                input.Type,
                input.Value,
                input.ExpiryDate,
                input.Quantity,
                input.MinOrderAmount,
                input.MaxDiscountAmount,
                input.Description,
                input.PromotionId);

            vouchers.Add(voucher);
            results.Add(new CreateVoucherResultDto { Id = voucher.Id, Code = voucher.Code });
        }

        await _voucherRepository.InsertManyAsync(vouchers);

        return new BulkCreateVoucherResultDto
        {
            TotalCreated = vouchers.Count,
            Vouchers = results
        };
    }

    public async Task<List<ExportVoucherDto>> ExportAsync()
    {
        var query = await _voucherRepository.GetQueryableAsync();
        var vouchers = await AsyncExecuter.ToListAsync(
            query.Where(v => !v.IsDeleted)
                .OrderByDescending(v => v.CreationTime));

        return vouchers.Select(v => new ExportVoucherDto
        {
            Id = v.Id,
            Code = v.Code,
            Type = v.Type.ToString(),
            Value = v.Value,
            ExpiryDate = v.ExpiryDate,
            TotalQuantity = v.TotalQuantity,
            UsedQuantity = v.UsedQuantity,
            IsActive = v.IsActive,
            Description = v.Description,
            MinOrderAmount = v.MinOrderAmount,
            MaxDiscountAmount = v.MaxDiscountAmount,
            PromotionId = v.PromotionId,
            CreatedAt = v.CreationTime
        }).ToList();
    }

    /// <summary>
    /// Generates a unique voucher code in format "KLC-XXXX-XXXX".
    /// Uses cryptographic random for uniqueness.
    /// </summary>
    private static string GenerateUniqueCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // exclude ambiguous chars
        Span<byte> randomBytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(randomBytes);

        var part1 = new char[4];
        var part2 = new char[4];

        for (var i = 0; i < 4; i++)
        {
            part1[i] = chars[randomBytes[i] % chars.Length];
            part2[i] = chars[randomBytes[i + 4] % chars.Length];
        }

        return $"KLC-{new string(part1)}-{new string(part2)}";
    }
}
