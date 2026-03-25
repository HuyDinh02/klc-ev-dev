using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Payments;
using KLC.Permissions;
using KLC.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Domain.Repositories;

namespace KLC.Controllers;

[ApiController]
[Route("api/v1/admin/wallet-transactions")]
[Authorize(KLCPermissions.Payments.Default)]
public class AdminWalletTransactionController : AbpController
{
    private readonly IRepository<WalletTransaction, Guid> _walletTransactionRepository;
    private readonly IRepository<AppUser, Guid> _appUserRepository;

    public AdminWalletTransactionController(
        IRepository<WalletTransaction, Guid> walletTransactionRepository,
        IRepository<AppUser, Guid> appUserRepository)
    {
        _walletTransactionRepository = walletTransactionRepository;
        _appUserRepository = appUserRepository;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AdminWalletTransactionDto>>> GetAllAsync(
        [FromQuery] WalletTransactionType? type,
        [FromQuery] TransactionStatus? status,
        [FromQuery] PaymentGateway? gateway,
        [FromQuery] int maxResultCount = 20,
        [FromQuery] int skipCount = 0)
    {
        var query = (await _walletTransactionRepository.GetQueryableAsync())
            .OrderByDescending(t => t.CreationTime);

        if (type.HasValue)
            query = (IOrderedQueryable<WalletTransaction>)query.Where(t => t.Type == type.Value);
        if (status.HasValue)
            query = (IOrderedQueryable<WalletTransaction>)query.Where(t => t.Status == status.Value);
        if (gateway.HasValue)
            query = (IOrderedQueryable<WalletTransaction>)query.Where(t => t.PaymentGateway == gateway.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip(skipCount)
            .Take(Math.Min(maxResultCount, 100))
            .ToListAsync();

        var userIds = items.Select(t => t.UserId).Distinct().ToList();
        var users = await (await _appUserRepository.GetQueryableAsync())
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        var dtos = items.Select(t => new AdminWalletTransactionDto
        {
            Id = t.Id,
            UserId = t.UserId,
            UserName = users.GetValueOrDefault(t.UserId, "Unknown"),
            Type = t.Type,
            Amount = t.Amount,
            BalanceAfter = t.BalanceAfter,
            PaymentGateway = t.PaymentGateway,
            GatewayTransactionId = t.GatewayTransactionId,
            Status = t.Status,
            Description = t.Description,
            ReferenceCode = t.ReferenceCode,
            CreationTime = t.CreationTime
        }).ToList();

        return Ok(new PagedResultDto<AdminWalletTransactionDto>(totalCount, dtos));
    }
}

public class AdminWalletTransactionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public WalletTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public PaymentGateway? PaymentGateway { get; set; }
    public string? GatewayTransactionId { get; set; }
    public TransactionStatus Status { get; set; }
    public string? Description { get; set; }
    public string? ReferenceCode { get; set; }
    public DateTime CreationTime { get; set; }
}
