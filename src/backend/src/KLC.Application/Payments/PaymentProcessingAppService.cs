using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Auditing;
using KLC.Enums;
using KLC.Marketing;
using KLC.Sessions;
using KLC.Users;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace KLC.Payments;

/// <summary>
/// Application service for session payment processing business logic.
/// Shared between Admin API and Driver BFF.
/// </summary>
public class PaymentProcessingAppService : KLCAppService, IPaymentProcessingAppService
{
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IRepository<Voucher, Guid> _voucherRepository;
    private readonly IRepository<UserVoucher, Guid> _userVoucherRepository;
    private readonly IRepository<WalletTransaction, Guid> _walletTransactionRepository;
    private readonly IRepository<AppUser, Guid> _appUserRepository;
    private readonly WalletDomainService _walletDomainService;
    private readonly IEnumerable<IPaymentGatewayService> _paymentGateways;
    private readonly IAuditEventLogger _auditLogger;

    public PaymentProcessingAppService(
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IRepository<Voucher, Guid> voucherRepository,
        IRepository<UserVoucher, Guid> userVoucherRepository,
        IRepository<WalletTransaction, Guid> walletTransactionRepository,
        IRepository<AppUser, Guid> appUserRepository,
        WalletDomainService walletDomainService,
        IEnumerable<IPaymentGatewayService> paymentGateways,
        IAuditEventLogger auditLogger)
    {
        _sessionRepository = sessionRepository;
        _paymentRepository = paymentRepository;
        _voucherRepository = voucherRepository;
        _userVoucherRepository = userVoucherRepository;
        _walletTransactionRepository = walletTransactionRepository;
        _appUserRepository = appUserRepository;
        _walletDomainService = walletDomainService;
        _paymentGateways = paymentGateways;
        _auditLogger = auditLogger;
    }

    public async Task<SessionPaymentResultDto> ProcessSessionPaymentAsync(ProcessSessionPaymentInput input)
    {
        var session = await _sessionRepository.FirstOrDefaultAsync(
            s => s.Id == input.SessionId && s.UserId == input.UserId);

        if (session == null)
        {
            return new SessionPaymentResultDto { Success = false, Error = "Session not found" };
        }

        if (session.Status != SessionStatus.Completed)
        {
            return new SessionPaymentResultDto { Success = false, Error = "Session not completed" };
        }

        // Check for existing payment
        var existingPayment = await _paymentRepository.FirstOrDefaultAsync(
            p => p.SessionId == input.SessionId && p.Status == PaymentStatus.Completed);

        if (existingPayment != null)
        {
            return new SessionPaymentResultDto { Success = false, Error = "Payment already processed" };
        }

        var sessionCost = session.TotalCost;
        decimal voucherDiscount = 0;
        Voucher? voucher = null;

        // Apply voucher discount if provided
        if (!string.IsNullOrWhiteSpace(input.VoucherCode))
        {
            voucher = await _voucherRepository.FirstOrDefaultAsync(v => v.Code == input.VoucherCode);

            if (voucher == null)
            {
                return new SessionPaymentResultDto { Success = false, Error = "Voucher not found" };
            }

            if (!voucher.IsValid())
            {
                return new SessionPaymentResultDto { Success = false, Error = "Voucher is not valid or has expired" };
            }

            // Check user hasn't already used this voucher
            var alreadyUsed = await _userVoucherRepository.AnyAsync(
                uv => uv.UserId == input.UserId && uv.VoucherId == voucher.Id && uv.IsUsed);

            if (alreadyUsed)
            {
                return new SessionPaymentResultDto { Success = false, Error = "You have already used this voucher" };
            }

            // Check minimum order amount
            if (voucher.MinOrderAmount.HasValue && sessionCost < voucher.MinOrderAmount.Value)
            {
                return new SessionPaymentResultDto
                {
                    Success = false,
                    Error = $"Minimum order amount is {voucher.MinOrderAmount.Value:N0}đ for this voucher"
                };
            }

            // Calculate discount
            voucherDiscount = voucher.Type switch
            {
                VoucherType.FixedAmount => Math.Min(voucher.Value, sessionCost),
                VoucherType.Percentage => Math.Min(
                    sessionCost * voucher.Value / 100m,
                    voucher.MaxDiscountAmount ?? sessionCost),
                VoucherType.FreeCharging => sessionCost,
                _ => 0
            };
        }

        var finalAmount = sessionCost - voucherDiscount;

        // Record voucher usage if applied
        if (voucher != null && voucherDiscount > 0)
        {
            var user = await _appUserRepository.FirstOrDefaultAsync(u => u.IdentityUserId == input.UserId);

            if (user == null)
            {
                return new SessionPaymentResultDto { Success = false, Error = "User not found" };
            }

            // Create voucher credit transaction
            var (_, voucherTransaction) = _walletDomainService.ApplyVoucher(
                user, voucherDiscount, $"Voucher {voucher.Code} discount for session");

            // Deduct immediately for the session (net effect: discount applied)
            user.DeductFromWallet(voucherDiscount);

            var userVoucher = new UserVoucher(GuidGenerator.Create(), input.UserId, voucher.Id);
            userVoucher.MarkUsed();
            voucher.IncrementUsage();

            await _walletTransactionRepository.InsertAsync(voucherTransaction);
            await _userVoucherRepository.InsertAsync(userVoucher);
            await _voucherRepository.UpdateAsync(voucher);
            await _appUserRepository.UpdateAsync(user);
        }

        // If fully covered by voucher or zero-cost session
        if (finalAmount <= 0)
        {
            var paymentGateway = voucher != null ? PaymentGateway.Voucher : input.Gateway;
            var payment = new PaymentTransaction(
                GuidGenerator.Create(),
                input.SessionId,
                input.UserId,
                paymentGateway,
                sessionCost);

            payment.MarkProcessing();
            var reference = voucher != null ? $"voucher:{voucher.Code}" : "zero-cost-session";
            payment.MarkCompleted(reference);

            await _paymentRepository.InsertAsync(payment);
            await CurrentUnitOfWork!.SaveChangesAsync();

            Logger.LogInformation(
                "Payment completed (no gateway needed): SessionId={SessionId}, VoucherCode={VoucherCode}, Amount={Amount}",
                input.SessionId, voucher?.Code, sessionCost);

            _auditLogger.LogPaymentEvent("PaymentCompleted", payment.Id, sessionCost, paymentGateway.ToString(), input.UserId.ToString());

            return new SessionPaymentResultDto
            {
                Success = true,
                PaymentId = payment.Id,
                Status = payment.Status,
                VoucherDiscount = voucherDiscount
            };
        }

        // Process remaining amount via gateway
        var gatewayPayment = new PaymentTransaction(
            GuidGenerator.Create(),
            input.SessionId,
            input.UserId,
            input.Gateway,
            finalAmount);

        gatewayPayment.MarkProcessing();

        _auditLogger.LogPaymentEvent("PaymentInitiated", gatewayPayment.Id, finalAmount, input.Gateway.ToString(), input.UserId.ToString());

        var gateway = _paymentGateways.FirstOrDefault(g => g.Gateway == input.Gateway);
        if (gateway == null)
        {
            gatewayPayment.MarkFailed($"Gateway {input.Gateway} not supported");
            await _paymentRepository.InsertAsync(gatewayPayment);
            await CurrentUnitOfWork!.SaveChangesAsync();
            _auditLogger.LogPaymentEvent("PaymentFailed", gatewayPayment.Id, finalAmount, input.Gateway.ToString(), input.UserId.ToString());
            return new SessionPaymentResultDto { Success = false, PaymentId = gatewayPayment.Id, Error = $"Gateway {input.Gateway} not supported" };
        }

        try
        {
            var gatewayResult = await gateway.CreateTopUpAsync(new CreateTopUpRequest
            {
                ReferenceCode = gatewayPayment.ReferenceCode,
                Amount = finalAmount,
                Description = $"Session payment #{gatewayPayment.ReferenceCode}",
                ReturnUrl = "klc://payment/callback",
                NotifyUrl = "/api/v1/payments/callback",
                ClientIpAddress = input.ClientIpAddress
            });

            if (gatewayResult.Success)
            {
                gatewayPayment.MarkCompleted(gatewayResult.GatewayTransactionId ?? "");
                _auditLogger.LogPaymentEvent("PaymentCompleted", gatewayPayment.Id, finalAmount, input.Gateway.ToString(), input.UserId.ToString());
            }
            else
            {
                gatewayPayment.MarkFailed(gatewayResult.ErrorMessage ?? "Payment failed");
                _auditLogger.LogPaymentEvent("PaymentFailed", gatewayPayment.Id, finalAmount, input.Gateway.ToString(), input.UserId.ToString());
            }

            await _paymentRepository.InsertAsync(gatewayPayment);
            await CurrentUnitOfWork!.SaveChangesAsync();

            return new SessionPaymentResultDto
            {
                Success = gatewayResult.Success,
                PaymentId = gatewayPayment.Id,
                Status = gatewayPayment.Status,
                RedirectUrl = gatewayResult.RedirectUrl,
                VoucherDiscount = voucherDiscount > 0 ? voucherDiscount : null,
                Error = gatewayResult.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Payment gateway error for session {SessionId}, gateway {Gateway}", input.SessionId, input.Gateway);
            gatewayPayment.MarkFailed("Payment processing error");
            await _paymentRepository.InsertAsync(gatewayPayment);
            await CurrentUnitOfWork!.SaveChangesAsync();
            return new SessionPaymentResultDto { Success = false, PaymentId = gatewayPayment.Id, Error = "Payment processing failed" };
        }
    }
}
