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
            .Where(u => userIds.Contains(u.Id) || userIds.Contains(u.IdentityUserId))
            .ToListAsync();
        var userMap = new Dictionary<Guid, string>();
        foreach (var u in users)
        {
            userMap.TryAdd(u.Id, u.FullName);
            userMap.TryAdd(u.IdentityUserId, u.FullName);
        }

        var dtos = items.Select(t => new AdminWalletTransactionDto
        {
            Id = t.Id,
            UserId = t.UserId,
            UserName = userMap.GetValueOrDefault(t.UserId, t.UserId == Guid.Empty ? "Walk-in" : "Unknown"),
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

    [HttpPost("{id:guid}/query-vnpay")]
    public async Task<ActionResult<VnPayQueryResultDto>> QueryVnPayAsync(
        Guid id,
        [FromServices] IEnumerable<IPaymentGatewayService> gateways,
        [FromServices] IRepository<AppUser, Guid> userRepo,
        [FromServices] WalletDomainService walletDomainService)
    {
        var txn = await _walletTransactionRepository.FirstOrDefaultAsync(t => t.Id == id);
        if (txn == null) return NotFound();
        if (txn.PaymentGateway != PaymentGateway.VnPay)
            return BadRequest(new { error = "Transaction is not a VnPay payment" });

        var vnpay = gateways.FirstOrDefault(g => g.Gateway == PaymentGateway.VnPay);
        if (vnpay == null) return BadRequest(new { error = "VnPay gateway not configured" });

        var result = await vnpay.QueryTransactionAsync(new QueryTransactionRequest
        {
            TxnRef = txn.ReferenceCode,
            TransactionDate = txn.CreationTime.AddHours(7).ToString("yyyyMMddHHmmss")
        });

        // Auto-reconcile if VnPay confirms success and transaction is still pending
        if (result.IsValid && result.IsSuccess && txn.Status == TransactionStatus.Pending)
        {
            var user = await userRepo.FirstOrDefaultAsync(u => u.Id == txn.UserId);
            if (user != null)
            {
                user.AddToWallet(txn.Amount);
                txn.MarkCompleted(result.GatewayTransactionId);
                await userRepo.UpdateAsync(user);
                await _walletTransactionRepository.UpdateAsync(txn);
            }
        }
        else if (result.IsValid && !result.IsSuccess && txn.Status == TransactionStatus.Pending)
        {
            txn.MarkFailed();
            await _walletTransactionRepository.UpdateAsync(txn);
        }

        return Ok(new VnPayQueryResultDto
        {
            IsValid = result.IsValid,
            IsSuccess = result.IsSuccess,
            GatewayTransactionId = result.GatewayTransactionId,
            ErrorMessage = result.ErrorMessage,
            TransactionStatus = txn.Status,
            Reconciled = txn.Status != TransactionStatus.Pending
        });
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

public class VnPayQueryResultDto
{
    public bool IsValid { get; set; }
    public bool IsSuccess { get; set; }
    public string? GatewayTransactionId { get; set; }
    public string? ErrorMessage { get; set; }
    public TransactionStatus TransactionStatus { get; set; }
    public bool Reconciled { get; set; }
}
