using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Auditing;
using KLC.Enums;
using KLC.Permissions;
using KLC.Sessions;
using KLC.Stations;
using KLC.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace KLC.Payments;

[Authorize]
public class PaymentAppService : KLCAppService, IPaymentAppService
{
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<UserPaymentMethod, Guid> _paymentMethodRepository;
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<AppUser, Guid> _appUserRepository;
    private readonly IRepository<WalletTransaction, Guid> _walletTransactionRepository;
    private readonly IEnumerable<IPaymentGatewayService> _gateways;
    private readonly WalletDomainService _walletDomainService;
    private readonly IPaymentCallbackValidator _callbackValidator;
    private readonly IAuditEventLogger _auditLogger;
    private readonly IInvoiceNumberService _invoiceNumberService;
    private readonly IAbpDistributedLock _distributedLock;

    public PaymentAppService(
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<UserPaymentMethod, Guid> paymentMethodRepository,
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<AppUser, Guid> appUserRepository,
        IRepository<WalletTransaction, Guid> walletTransactionRepository,
        IEnumerable<IPaymentGatewayService> gateways,
        WalletDomainService walletDomainService,
        IPaymentCallbackValidator callbackValidator,
        IAuditEventLogger auditLogger,
        IInvoiceNumberService invoiceNumberService,
        IAbpDistributedLock distributedLock)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _sessionRepository = sessionRepository;
        _stationRepository = stationRepository;
        _appUserRepository = appUserRepository;
        _walletTransactionRepository = walletTransactionRepository;
        _gateways = gateways;
        _walletDomainService = walletDomainService;
        _callbackValidator = callbackValidator;
        _auditLogger = auditLogger;
        _invoiceNumberService = invoiceNumberService;
        _distributedLock = distributedLock;
    }

    public async Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto input)
    {
        var userId = CurrentUser.GetId();
        var session = await _sessionRepository.GetAsync(input.SessionId);

        // Validate session belongs to user
        if (session.UserId != userId)
        {
            throw new BusinessException(KLCDomainErrorCodes.Payment.SessionNotOwned);
        }

        // Check session is completed
        if (session.Status != SessionStatus.Completed)
        {
            throw new BusinessException(KLCDomainErrorCodes.Payment.SessionNotCompleted);
        }

        // Check for existing payment
        var existingPayment = await _paymentRepository.FirstOrDefaultAsync(
            p => p.SessionId == input.SessionId &&
                 (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Processing || p.Status == PaymentStatus.Completed));

        if (existingPayment != null)
        {
            if (existingPayment.Status == PaymentStatus.Completed)
            {
                throw new BusinessException(KLCDomainErrorCodes.Payment.AlreadyCompleted);
            }
            return new PaymentResultDto
            {
                PaymentId = existingPayment.Id,
                Status = existingPayment.Status,
                ReferenceCode = existingPayment.ReferenceCode
            };
        }

        // Create payment transaction
        var payment = new PaymentTransaction(
            GuidGenerator.Create(),
            input.SessionId,
            userId,
            input.Gateway,
            session.TotalCost
        );

        await _paymentRepository.InsertAsync(payment);

        _auditLogger.LogPaymentEvent("PaymentInitiated", payment.Id, session.TotalCost, input.Gateway.ToString(), userId.ToString());

        var gateway = _gateways.FirstOrDefault(g => g.Gateway == input.Gateway);
        string? redirectUrl = null;

        if (gateway != null)
        {
            var gatewayResult = await gateway.CreateTopUpAsync(new CreateTopUpRequest
            {
                ReferenceCode = payment.ReferenceCode,
                Amount = session.TotalCost,
                Description = $"Session payment: {payment.ReferenceCode}",
                ReturnUrl = $"/payments/{payment.Id}/result",
                NotifyUrl = $"/api/payments/callback/{input.Gateway}",
                ClientIpAddress = input.ClientIpAddress
            });
            redirectUrl = gatewayResult.RedirectUrl;
        }

        return new PaymentResultDto
        {
            PaymentId = payment.Id,
            Status = payment.Status,
            GatewayRedirectUrl = redirectUrl,
            ReferenceCode = payment.ReferenceCode
        };
    }

    public async Task<PaymentTransactionDto> GetAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var payment = await _paymentRepository.GetAsync(id);

        // Check ownership (unless admin)
        if (payment.UserId != userId && !await AuthorizationService.IsGrantedAsync(KLCPermissions.Payments.ViewAll))
        {
            throw new BusinessException(KLCDomainErrorCodes.Payment.NotOwned);
        }

        var session = await _sessionRepository.GetAsync(payment.SessionId);
        var station = await _stationRepository.GetAsync(session.StationId);

        return MapToDto(payment, station.Name, session.TotalEnergyKwh);
    }

    public async Task<PagedResultDto<PaymentListDto>> GetHistoryAsync(GetPaymentListDto input)
    {
        var userId = CurrentUser.GetId();
        var query = await _paymentRepository.GetQueryableAsync();

        // Filter by user unless admin
        if (!await AuthorizationService.IsGrantedAsync(KLCPermissions.Payments.ViewAll))
        {
            query = query.Where(p => p.UserId == userId);
        }

        if (input.Status.HasValue)
        {
            query = query.Where(p => p.Status == input.Status.Value);
        }

        if (input.Gateway.HasValue)
        {
            query = query.Where(p => p.Gateway == input.Gateway.Value);
        }

        if (input.FromDate.HasValue)
        {
            query = query.Where(p => p.CreationTime >= input.FromDate.Value);
        }

        if (input.ToDate.HasValue)
        {
            query = query.Where(p => p.CreationTime <= input.ToDate.Value);
        }

        if (input.Cursor.HasValue)
        {
            query = query.Where(p => p.Id.CompareTo(input.Cursor.Value) > 0);
        }

        query = query.OrderByDescending(p => p.CreationTime);

        var totalCount = await AsyncExecuter.CountAsync(query);
        var payments = await AsyncExecuter.ToListAsync(query.Take(input.MaxResultCount));

        // Get station names and user names
        var sessionIds = payments.Select(p => p.SessionId).Distinct().ToList();
        var sessions = await _sessionRepository.GetListAsync(s => sessionIds.Contains(s.Id));
        var stationIds = sessions.Select(s => s.StationId).Distinct().ToList();
        var stations = await _stationRepository.GetListAsync(st => stationIds.Contains(st.Id));

        var userIds = payments.Select(p => p.UserId).Distinct().ToList();
        var users = await _appUserRepository.GetListAsync(u => userIds.Contains(u.IdentityUserId));
        var userMap = users.ToDictionary(u => u.IdentityUserId, u => u.FullName ?? u.PhoneNumber);

        var sessionMap = sessions.ToDictionary(s => s.Id);
        var stationMap = stations.ToDictionary(st => st.Id, st => st.Name);

        var dtos = payments.Select(p =>
        {
            var session = sessionMap.TryGetValue(p.SessionId, out var s) ? s : null;
            var stationName = session != null && stationMap.TryGetValue(session.StationId, out var name) ? name : null;
            return new PaymentListDto
            {
                Id = p.Id,
                SessionId = p.SessionId,
                Gateway = p.Gateway,
                Amount = p.Amount,
                Status = p.Status,
                ReferenceCode = p.ReferenceCode,
                CreationTime = p.CreationTime,
                StationName = stationName,
                UserName = userMap.GetValueOrDefault(p.UserId)
            };
        }).ToList();

        return new PagedResultDto<PaymentListDto>(totalCount, dtos);
    }

    public async Task<PaymentMethodDto> AddPaymentMethodAsync(CreatePaymentMethodDto input)
    {
        var userId = CurrentUser.GetId();

        // If setting as default, remove default from other methods
        if (input.IsDefault)
        {
            var existingMethods = await _paymentMethodRepository.GetListAsync(
                m => m.UserId == userId && m.IsDefault);
            foreach (var method in existingMethods)
            {
                method.RemoveDefault();
            }
            if (existingMethods.Any())
            {
                await _paymentMethodRepository.UpdateManyAsync(existingMethods);
            }
        }

        var paymentMethod = new UserPaymentMethod(
            GuidGenerator.Create(),
            userId,
            input.Gateway,
            input.DisplayName,
            input.TokenReference
        );

        if (input.IsDefault)
        {
            paymentMethod.SetAsDefault();
        }

        await _paymentMethodRepository.InsertAsync(paymentMethod);

        return MapToMethodDto(paymentMethod);
    }

    public async Task<List<PaymentMethodDto>> GetPaymentMethodsAsync()
    {
        var userId = CurrentUser.GetId();
        var methods = await _paymentMethodRepository.GetListAsync(
            m => m.UserId == userId && m.IsActive);

        return methods.Select(MapToMethodDto).ToList();
    }

    public async Task DeletePaymentMethodAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var method = await _paymentMethodRepository.GetAsync(id);

        if (method.UserId != userId)
        {
            throw new BusinessException(KLCDomainErrorCodes.Payment.MethodNotOwned);
        }

        method.Deactivate();
        await _paymentMethodRepository.UpdateAsync(method);
    }

    public async Task SetDefaultPaymentMethodAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var method = await _paymentMethodRepository.GetAsync(id);

        if (method.UserId != userId)
        {
            throw new BusinessException(KLCDomainErrorCodes.Payment.MethodNotOwned);
        }

        // Remove default from other methods
        var otherMethods = await _paymentMethodRepository.GetListAsync(
            m => m.UserId == userId && m.IsDefault && m.Id != id);
        foreach (var other in otherMethods)
        {
            other.RemoveDefault();
        }
        if (otherMethods.Any())
        {
            await _paymentMethodRepository.UpdateManyAsync(otherMethods);
        }

        method.SetAsDefault();
        await _paymentMethodRepository.UpdateAsync(method);
    }

    public async Task<InvoiceDto> GetInvoiceAsync(Guid id)
    {
        var invoice = await _invoiceRepository.GetAsync(id);
        var payment = await _paymentRepository.GetAsync(invoice.PaymentTransactionId);
        // IDOR check: verify current user owns this payment
        if (payment.UserId != CurrentUser.GetId())
            throw new BusinessException("KLC:Payment:NotOwned");
        var session = await _sessionRepository.GetAsync(payment.SessionId);
        var station = await _stationRepository.GetAsync(session.StationId);

        return MapToInvoiceDto(invoice, station.Name, session.StartTime, session.EndTime);
    }

    public async Task<InvoiceDto?> GetInvoiceByPaymentAsync(Guid paymentId)
    {
        var invoice = await _invoiceRepository.FirstOrDefaultAsync(i => i.PaymentTransactionId == paymentId);
        if (invoice == null) return null;

        var payment = await _paymentRepository.GetAsync(paymentId);
        // IDOR check: verify current user owns this payment
        if (payment.UserId != CurrentUser.GetId())
            throw new BusinessException("KLC:Payment:NotOwned");
        var session = await _sessionRepository.GetAsync(payment.SessionId);
        var station = await _stationRepository.GetAsync(session.StationId);

        return MapToInvoiceDto(invoice, station.Name, session.StartTime, session.EndTime);
    }

    [AllowAnonymous]
    public async Task HandleCallbackAsync(string gateway, PaymentCallbackDto callback)
    {
        // Find payment by reference code
        var payment = await _paymentRepository.FirstOrDefaultAsync(
            p => p.ReferenceCode == callback.ReferenceCode);

        if (payment == null)
        {
            throw new BusinessException(KLCDomainErrorCodes.Payment.NotFound)
                .WithData("referenceCode", callback.ReferenceCode);
        }

        // Verify callback signature and amount via shared validator
        var rawData = callback.RawData
            ?? $"{callback.ReferenceCode}{callback.Status}{callback.TransactionId}";

        var validation = await _callbackValidator.ValidateGatewayCallbackAsync(
            payment.Gateway, rawData, callback.Signature, payment.Amount);

        if (!validation.IsValid)
        {
            Logger.LogWarning(
                "Payment callback validation failed: Gateway={Gateway}, Ref={ReferenceCode}, Error={Error}",
                payment.Gateway, callback.ReferenceCode, validation.ErrorMessage);

            var errorCode = validation.ErrorMessage?.Contains("Amount") == true
                ? KLCDomainErrorCodes.Payment.InvalidAmount
                : KLCDomainErrorCodes.Payment.InvalidSignature;

            throw new BusinessException(errorCode)
                .WithData("gateway", payment.Gateway.ToString());
        }

        if (callback.Status == "success" || callback.Status == "completed")
        {
            payment.MarkCompleted(callback.TransactionId ?? "");

            _auditLogger.LogPaymentEvent("PaymentCompleted", payment.Id, payment.Amount, payment.Gateway.ToString(), payment.UserId.ToString());

            // Generate invoice
            var session = await _sessionRepository.GetAsync(payment.SessionId);
            var invoiceNumber = await _invoiceNumberService.GenerateNextAsync();
            var invoice = new Invoice(
                GuidGenerator.Create(),
                payment.Id,
                invoiceNumber,
                session.TotalEnergyKwh,
                session.RatePerKwh,
                10m // 10% VAT
            );

            await _invoiceRepository.InsertAsync(invoice);
        }
        else if (callback.Status == "failed" || callback.Status == "error")
        {
            payment.MarkFailed(callback.ErrorCode ?? "Payment failed");

            _auditLogger.LogPaymentEvent("PaymentFailed", payment.Id, payment.Amount, payment.Gateway.ToString(), payment.UserId.ToString());
        }

        await _paymentRepository.UpdateAsync(payment);
    }

    [AllowAnonymous]
    public async Task<VnPayIpnResponse> HandleVnPayIpnAsync(Dictionary<string, string> queryParams)
    {
        // Step 1: Find payment by TxnRef
        var txnRef = queryParams.GetValueOrDefault("vnp_TxnRef");
        if (string.IsNullOrEmpty(txnRef))
            return VnPayIpnResponse.OrderNotFound();

        // Idempotency lock: VnPay retries IPN on timeout — prevent double invoice creation.
        // Uses per-txnRef key so unrelated payments are not blocked.
        await using var lockHandle = await _distributedLock.TryAcquireAsync(
            $"ipn:payment:{txnRef}", TimeSpan.FromSeconds(60));
        if (lockHandle == null)
        {
            Logger.LogWarning("[VnPay IPN] Concurrent IPN for TxnRef={TxnRef} — skipping duplicate", txnRef);
            return VnPayIpnResponse.AlreadyConfirmed();
        }

        var payment = await _paymentRepository.FirstOrDefaultAsync(p => p.ReferenceCode == txnRef);
        if (payment == null)
        {
            Logger.LogWarning("[VnPay IPN] Order not found: TxnRef={TxnRef}", txnRef);
            return VnPayIpnResponse.OrderNotFound();
        }

        if (payment.Status == PaymentStatus.Completed)
            return VnPayIpnResponse.AlreadyConfirmed();

        // Step 2: Validate signature and amount via shared validator
        var validation = await _callbackValidator.ValidateVnPayIpnAsync(queryParams, payment.Amount);
        if (!validation.IsValid)
        {
            Logger.LogWarning("[VnPay IPN] Validation failed: {Error}, TxnRef={TxnRef}",
                validation.ErrorMessage, txnRef);
            return validation.ErrorMessage?.Contains("Amount") == true
                ? VnPayIpnResponse.InvalidAmount()
                : VnPayIpnResponse.InvalidSignature();
        }

        // Step 3: Update payment status
        if (validation.IsPaymentSuccess)
        {
            payment.MarkCompleted(validation.GatewayTransactionId ?? "");
            _auditLogger.LogPaymentEvent("PaymentCompleted", payment.Id, payment.Amount, payment.Gateway.ToString(), payment.UserId.ToString());

            // Generate invoice — sequence is atomic at DB level, safe under concurrency
            var session = await _sessionRepository.GetAsync(payment.SessionId);
            var invoiceNumber = await _invoiceNumberService.GenerateNextAsync();
            var invoice = new Invoice(
                GuidGenerator.Create(),
                payment.Id,
                invoiceNumber,
                session.TotalEnergyKwh,
                session.RatePerKwh,
                10m // 10% VAT
            );
            await _invoiceRepository.InsertAsync(invoice);
        }
        else
        {
            payment.MarkFailed($"VnPay response code: {validation.ResponseCode}");
            _auditLogger.LogPaymentEvent("PaymentFailed", payment.Id, payment.Amount, payment.Gateway.ToString(), payment.UserId.ToString());
        }

        await _paymentRepository.UpdateAsync(payment);

        Logger.LogInformation(
            "[VnPay IPN] Processed: TxnRef={TxnRef}, ResponseCode={ResponseCode}, PaymentStatus={Status}",
            txnRef, validation.ResponseCode, payment.Status);

        return VnPayIpnResponse.Success();
    }

    public async Task<PaymentTransactionDto> QueryVnPayTransactionAsync(Guid paymentId)
    {
        var payment = await _paymentRepository.GetAsync(paymentId);
        // IDOR check: verify current user owns this payment
        if (payment.UserId != CurrentUser.GetId())
            throw new BusinessException("KLC:Payment:NotOwned");

        var vnpayGateway = _gateways.FirstOrDefault(g => g.Gateway == PaymentGateway.VnPay);
        if (vnpayGateway == null)
        {
            throw new BusinessException(KLCDomainErrorCodes.Payment.GatewayNotSupported)
                .WithData("gateway", "VnPay");
        }

        var queryResult = await vnpayGateway.QueryTransactionAsync(new QueryTransactionRequest
        {
            TxnRef = payment.ReferenceCode,
            TransactionDate = payment.CreationTime.AddHours(7).ToString("yyyyMMddHHmmss"),
            GatewayTransactionId = payment.GatewayTransactionId,
            OrderInfo = $"Query for {payment.ReferenceCode}"
        });

        // If the query reveals a completed transaction but our record is still pending, update it
        if (queryResult.IsValid && queryResult.IsSuccess && payment.Status == PaymentStatus.Pending)
        {
            payment.MarkCompleted(queryResult.GatewayTransactionId ?? "");
            await _paymentRepository.UpdateAsync(payment);

            _auditLogger.LogPaymentEvent("PaymentReconciled", payment.Id, payment.Amount, payment.Gateway.ToString(), payment.UserId.ToString());

            Logger.LogInformation(
                "[VnPay] Payment reconciled via querydr: PaymentId={PaymentId}, TxnRef={TxnRef}",
                payment.Id, payment.ReferenceCode);
        }

        var session = await _sessionRepository.GetAsync(payment.SessionId);
        var station = await _stationRepository.GetAsync(session.StationId);

        return MapToDto(payment, station.Name, session.TotalEnergyKwh);
    }

    [Authorize(KLCPermissions.Payments.Refund)]
    public async Task<RefundResultDto> RefundAsync(Guid transactionId, RefundInput input)
    {
        var payment = await _paymentRepository.GetAsync(transactionId);

        // Call VnPay refund API FIRST — if it fails, do not credit wallet
        if (payment.Gateway == PaymentGateway.VnPay && !string.IsNullOrEmpty(payment.GatewayTransactionId))
        {
            var vnpayGateway = _gateways.FirstOrDefault(g => g.Gateway == PaymentGateway.VnPay);
            if (vnpayGateway != null)
            {
                var refundResult = await vnpayGateway.RefundAsync(new RefundGatewayRequest
                {
                    TxnRef = payment.ReferenceCode,
                    GatewayTransactionId = payment.GatewayTransactionId,
                    Amount = payment.Amount,
                    TransactionDate = payment.CreationTime.AddHours(7).ToString("yyyyMMddHHmmss"),
                    CreatedBy = CurrentUser.UserName ?? "admin",
                    OrderInfo = input.Reason ?? $"Refund for {payment.ReferenceCode}"
                });

                if (!refundResult.Success)
                {
                    throw new BusinessException(KLCDomainErrorCodes.Payment.RefundFailed)
                        .WithData("paymentId", payment.Id)
                        .WithData("error", refundResult.ErrorMessage);
                }

                Logger.LogInformation(
                    "[VnPay] Gateway refund succeeded: PaymentId={PaymentId}, TxnRef={TxnRef}",
                    payment.Id, payment.ReferenceCode);
            }
        }

        // Only credit wallet after gateway confirms refund (or for non-VnPay payments)
        payment.MarkRefunded();

        var user = await _appUserRepository.FirstOrDefaultAsync(u => u.IdentityUserId == payment.UserId)
            ?? throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);

        var (newBalance, walletTransaction) = _walletDomainService.Refund(
            user,
            payment.Amount,
            sessionId: payment.SessionId,
            description: input.Reason ?? $"Refund for payment {payment.ReferenceCode}");

        await _walletTransactionRepository.InsertAsync(walletTransaction);
        await _appUserRepository.UpdateAsync(user);
        await _paymentRepository.UpdateAsync(payment);

        _auditLogger.LogPaymentEvent("RefundProcessed", payment.Id, payment.Amount, payment.Gateway.ToString(), payment.UserId.ToString());

        return new RefundResultDto
        {
            PaymentId = payment.Id,
            WalletTransactionId = walletTransaction.Id,
            RefundAmount = payment.Amount,
            NewWalletBalance = newBalance,
            NewStatus = payment.Status
        };
    }

    private static PaymentTransactionDto MapToDto(PaymentTransaction payment, string? stationName, decimal energyKwh)
    {
        return new PaymentTransactionDto
        {
            Id = payment.Id,
            SessionId = payment.SessionId,
            UserId = payment.UserId,
            Gateway = payment.Gateway,
            Amount = payment.Amount,
            Status = payment.Status,
            GatewayTransactionId = payment.GatewayTransactionId,
            ReferenceCode = payment.ReferenceCode,
            ErrorMessage = payment.ErrorMessage,
            CreationTime = payment.CreationTime,
            CompletedAt = payment.CompletedAt,
            StationName = stationName,
            EnergyKwh = energyKwh
        };
    }

    private static PaymentMethodDto MapToMethodDto(UserPaymentMethod method)
    {
        return new PaymentMethodDto
        {
            Id = method.Id,
            Gateway = method.Gateway,
            DisplayName = method.DisplayName,
            IsDefault = method.IsDefault,
            LastFourDigits = method.LastFourDigits,
            CreationTime = method.CreationTime
        };
    }

    private static InvoiceDto MapToInvoiceDto(Invoice invoice, string? stationName, DateTime? startTime, DateTime? endTime)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            PaymentTransactionId = invoice.PaymentTransactionId,
            InvoiceNumber = invoice.InvoiceNumber,
            EnergyKwh = invoice.EnergyKwh,
            BaseAmount = invoice.BaseAmount,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            TaxRatePercent = invoice.TaxRatePercent,
            RatePerKwh = invoice.RatePerKwh,
            IssuedAt = invoice.IssuedAt,
            StationName = stationName,
            SessionStartTime = startTime,
            SessionEndTime = endTime
        };
    }
}
