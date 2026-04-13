using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Configuration;
using KLC.Enums;
using KLC.Notifications;
using KLC.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain.Repositories;

namespace KLC.Payments;

/// <summary>
/// Application service encapsulating wallet business logic: top-up initiation,
/// payment callback processing, and wallet crediting. Shared between Admin API and Driver BFF.
/// </summary>
public class WalletAppService : KLCAppService, IWalletAppService
{
    /// <summary>
    /// SBV Circular 41/2025: Monthly e-wallet top-up cap.
    /// </summary>
    private const decimal MonthlyTopUpLimit = 100_000_000m;

    private readonly IRepository<WalletTransaction, Guid> _walletTransactionRepository;
    private readonly IRepository<AppUser, Guid> _appUserRepository;
    private readonly IRepository<Notification, Guid> _notificationRepository;
    private readonly WalletDomainService _walletDomainService;
    private readonly IEnumerable<IPaymentGatewayService> _paymentGateways;
    private readonly IPaymentCallbackValidator _callbackValidator;
    private readonly IConfiguration _configuration;
    private readonly WalletSettings _walletSettings;
    private readonly IAbpDistributedLock _distributedLock;

    public WalletAppService(
        IRepository<WalletTransaction, Guid> walletTransactionRepository,
        IRepository<AppUser, Guid> appUserRepository,
        IRepository<Notification, Guid> notificationRepository,
        WalletDomainService walletDomainService,
        IEnumerable<IPaymentGatewayService> paymentGateways,
        IPaymentCallbackValidator callbackValidator,
        IConfiguration configuration,
        IOptions<WalletSettings> walletSettings,
        IAbpDistributedLock distributedLock)
    {
        _walletTransactionRepository = walletTransactionRepository;
        _appUserRepository = appUserRepository;
        _notificationRepository = notificationRepository;
        _walletDomainService = walletDomainService;
        _paymentGateways = paymentGateways;
        _callbackValidator = callbackValidator;
        _configuration = configuration;
        _walletSettings = walletSettings.Value;
        _distributedLock = distributedLock;
    }

    public async Task<TopUpInitiationResultDto> InitiateTopUpAsync(InitiateTopUpInput input)
    {
        if (input.Amount <= 0)
        {
            return new TopUpInitiationResultDto { Success = false, Error = "Amount must be positive" };
        }

        if (input.Amount < _walletSettings.MinTopUpAmount)
        {
            return new TopUpInitiationResultDto { Success = false, Error = KLCDomainErrorCodes.Wallet.MinTopUpAmount };
        }

        if (input.Amount > _walletSettings.MaxTopUpAmount)
        {
            return new TopUpInitiationResultDto { Success = false, Error = KLCDomainErrorCodes.Wallet.MaxTopUpAmount };
        }

        // SBV Circular 41/2025: Monthly top-up limit of 100,000,000 VND
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryable = await _walletTransactionRepository.GetQueryableAsync();
        var monthlyTotal = await AsyncExecuter.SumAsync(
            queryable.Where(t => t.UserId == input.UserId
                && t.Type == WalletTransactionType.TopUp
                && t.Status == TransactionStatus.Completed
                && t.CreationTime >= monthStart)
                .Select(t => (decimal?)t.Amount)) ?? 0;

        var remaining = MonthlyTopUpLimit - monthlyTotal;
        if (monthlyTotal + input.Amount > MonthlyTopUpLimit)
        {
            return new TopUpInitiationResultDto
            {
                Success = false,
                Error = $"Vượt quá hạn mức nạp {remaining:N0}đ/tháng theo quy định SBV"
            };
        }

        var user = await _appUserRepository.FirstOrDefaultAsync(u => u.IdentityUserId == input.UserId);
        if (user == null)
        {
            return new TopUpInitiationResultDto { Success = false, Error = "User not found" };
        }

        try
        {
            // Create a pending transaction first (gateway will confirm later)
            var transaction = new WalletTransaction(
                GuidGenerator.Create(),
                input.UserId,
                WalletTransactionType.TopUp,
                input.Amount,
                user.WalletBalance, // Balance not yet updated
                input.Gateway,
                description: $"Top-up via {input.Gateway}");

            await _walletTransactionRepository.InsertAsync(transaction);

            // Call payment gateway to get redirect URL
            var gateway = _paymentGateways.FirstOrDefault(g => g.Gateway == input.Gateway);
            var gatewayResult = gateway != null
                ? await gateway.CreateTopUpAsync(new CreateTopUpRequest
                {
                    ReferenceCode = transaction.ReferenceCode,
                    Amount = input.Amount,
                    Description = $"Top-up via {input.Gateway}",
                    ReturnUrl = _configuration["Payment:VnPay:ReturnUrl"] ?? "klc://wallet/topup/callback",
                    NotifyUrl = _configuration["Payment:VnPay:IpnUrl"]
                        ?? $"{_configuration["App:SelfUrl"] ?? "https://bff.ev.odcall.com"}/api/v1/wallet/topup/vnpay-ipn",
                    ClientIpAddress = input.ClientIpAddress,
                    BankCode = input.BankCode
                })
                : PaymentGatewayResult.Fail($"Gateway {input.Gateway} not supported");

            if (!gatewayResult.Success)
            {
                return new TopUpInitiationResultDto { Success = false, Error = gatewayResult.ErrorMessage };
            }

            Logger.LogInformation(
                "Top-up initiated: UserId={UserId}, Amount={Amount}, Gateway={Gateway}, TransactionId={TransactionId}",
                input.UserId, input.Amount, input.Gateway, transaction.Id);

            return new TopUpInitiationResultDto
            {
                Success = true,
                TransactionId = transaction.Id,
                ReferenceCode = transaction.ReferenceCode,
                RedirectUrl = gatewayResult.RedirectUrl!,
                Status = transaction.Status
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initiate top-up for user {UserId}", input.UserId);
            return new TopUpInitiationResultDto { Success = false, Error = "Failed to initiate top-up" };
        }
    }

    public async Task<TopUpCallbackResultAppDto> ProcessTopUpCallbackAsync(ProcessTopUpCallbackInput input)
    {
        var transaction = await _walletTransactionRepository.FirstOrDefaultAsync(
            t => t.ReferenceCode == input.ReferenceCode && t.Status == TransactionStatus.Pending);

        if (transaction == null)
        {
            Logger.LogWarning("Top-up callback for unknown reference: {ReferenceCode}", input.ReferenceCode);
            return new TopUpCallbackResultAppDto { Success = false, Error = "Transaction not found" };
        }

        if (input.Status == TransactionStatus.Completed)
        {
            var user = await _appUserRepository.FirstOrDefaultAsync(u => u.IdentityUserId == transaction.UserId);
            if (user == null)
            {
                return new TopUpCallbackResultAppDto { Success = false, Error = "User not found" };
            }

            // Use domain service for the actual wallet credit
            var (newBalance, _) = _walletDomainService.TopUp(
                user,
                transaction.Amount,
                transaction.PaymentGateway ?? PaymentGateway.Wallet,
                input.GatewayTransactionId);

            // Mark original pending transaction as completed
            transaction.MarkCompleted(input.GatewayTransactionId);
            await _walletTransactionRepository.UpdateAsync(transaction);
            await _appUserRepository.UpdateAsync(user);

            Logger.LogInformation(
                "Top-up completed: UserId={UserId}, Amount={Amount}, NewBalance={NewBalance}",
                transaction.UserId, transaction.Amount, newBalance);

            return new TopUpCallbackResultAppDto
            {
                Success = true,
                TransactionId = transaction.Id,
                NewBalance = newBalance,
                UserId = transaction.UserId,
                Amount = transaction.Amount
            };
        }
        else if (input.Status == TransactionStatus.Failed)
        {
            transaction.MarkFailed();
            await _walletTransactionRepository.UpdateAsync(transaction);

            Logger.LogWarning(
                "Top-up failed: UserId={UserId}, ReferenceCode={ReferenceCode}",
                transaction.UserId, input.ReferenceCode);

            return new TopUpCallbackResultAppDto
            {
                Success = false,
                TransactionId = transaction.Id,
                UserId = transaction.UserId,
                Amount = transaction.Amount,
                Error = "Payment gateway reported failure"
            };
        }

        return new TopUpCallbackResultAppDto { Success = false, Error = "Invalid callback status" };
    }

    public async Task<VnPayIpnProcessingResult> ProcessVnPayIpnAsync(Dictionary<string, string> queryParams)
    {
        // Step 1: Find wallet transaction by TxnRef
        var txnRef = queryParams.GetValueOrDefault("vnp_TxnRef");
        if (string.IsNullOrEmpty(txnRef))
            return new VnPayIpnProcessingResult { IpnResponse = VnPayIpnResponse.OrderNotFound() };

        var transaction = await _walletTransactionRepository.FirstOrDefaultAsync(t => t.ReferenceCode == txnRef);
        if (transaction == null)
        {
            Logger.LogWarning("[VnPay IPN] Wallet transaction not found: TxnRef={TxnRef}", txnRef);
            return new VnPayIpnProcessingResult { IpnResponse = VnPayIpnResponse.OrderNotFound() };
        }

        // Step 2: Validate signature and amount BEFORE idempotency check
        // VnPay sandbox tests invalid checksum + invalid amount sequentially on the
        // same TxnRef. Validation must return correct error codes regardless of lock state.
        var validation = await _callbackValidator.ValidateVnPayIpnAsync(queryParams, transaction.Amount);
        if (!validation.IsValid)
        {
            Logger.LogWarning("[VnPay IPN] Validation failed: {Error}, TxnRef={TxnRef}",
                validation.ErrorMessage, txnRef);
            var ipnResponse = validation.ErrorMessage?.Contains("Amount") == true
                ? VnPayIpnResponse.InvalidAmount()
                : VnPayIpnResponse.InvalidSignature();
            return new VnPayIpnProcessingResult { IpnResponse = ipnResponse };
        }

        // Step 3: Check if already completed
        if (transaction.Status == TransactionStatus.Completed)
            return new VnPayIpnProcessingResult { IpnResponse = VnPayIpnResponse.AlreadyConfirmed() };

        // Step 4: Idempotency lock — only needed for valid requests that will credit wallet.
        // Prevents double-credit from VnPay's at-least-once delivery guarantee.
        await using var lockHandle = await _distributedLock.TryAcquireAsync(
            $"ipn:wallet:{txnRef}", TimeSpan.FromSeconds(60));
        if (lockHandle == null)
        {
            Logger.LogWarning(
                "[VnPay IPN] Concurrent IPN for TxnRef={TxnRef} — skipping duplicate", txnRef);
            return new VnPayIpnProcessingResult { IpnResponse = VnPayIpnResponse.AlreadyConfirmed() };
        }

        // Step 5: Process based on response code
        if (validation.IsPaymentSuccess)
        {
            var user = await _appUserRepository.FirstOrDefaultAsync(u => u.IdentityUserId == transaction.UserId);
            if (user == null)
            {
                Logger.LogWarning("[VnPay IPN] User not found for transaction {TxnRef}", txnRef);
                return new VnPayIpnProcessingResult { IpnResponse = VnPayIpnResponse.UnknownError() };
            }

            // Retry loop handles the rare case where a concurrent operation (e.g.
            // a session payment) modified the user row between our read and save.
            decimal newBalance = 0;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        // Reload the user so concurrency token is fresh before retrying
                        user = await _appUserRepository.GetAsync(user.Id);
                    }

                    var (balance, _) = _walletDomainService.TopUp(
                        user,
                        transaction.Amount,
                        transaction.PaymentGateway ?? PaymentGateway.VnPay,
                        validation.GatewayTransactionId);
                    newBalance = balance;

                    transaction.MarkCompleted(validation.GatewayTransactionId);
                    await _walletTransactionRepository.UpdateAsync(transaction);
                    await _appUserRepository.UpdateAsync(user);
                    break;
                }
                catch (AbpDbConcurrencyException) when (attempt < 2)
                {
                    Logger.LogWarning(
                        "[VnPay IPN] Concurrency conflict on wallet update for {UserId}, retrying (attempt {Attempt})",
                        transaction.UserId, attempt + 1);
                }
            }

            // Create in-app notification for successful top-up
            var successNotification = new Notification(
                GuidGenerator.Create(),
                transaction.UserId,
                NotificationType.WalletTopUp,
                "Nạp ví thành công",
                $"Bạn đã nạp thành công {transaction.Amount:N0}đ vào ví. Số dư hiện tại: {newBalance:N0}đ.");
            await _notificationRepository.InsertAsync(successNotification);

            Logger.LogInformation(
                "[VnPay IPN] Wallet top-up completed: UserId={UserId}, Amount={Amount}, NewBalance={NewBalance}",
                transaction.UserId, transaction.Amount, newBalance);

            return new VnPayIpnProcessingResult
            {
                IpnResponse = VnPayIpnResponse.Success(),
                Completion = new TopUpCompletionResultDto
                {
                    Success = true,
                    TransactionId = transaction.Id,
                    UserId = transaction.UserId,
                    Amount = transaction.Amount,
                    NewBalance = newBalance
                }
            };
        }
        else
        {
            transaction.MarkFailed();
            await _walletTransactionRepository.UpdateAsync(transaction);

            // Create in-app notification for payment failure
            var failNotification = new Notification(
                GuidGenerator.Create(),
                transaction.UserId,
                NotificationType.PaymentFailed,
                "Nạp ví thất bại",
                $"Giao dịch nạp {transaction.Amount:N0}đ qua VnPay không thành công (mã: {validation.ResponseCode}). Vui lòng thử lại.");
            await _notificationRepository.InsertAsync(failNotification);

            Logger.LogWarning(
                "[VnPay IPN] Wallet top-up failed: TxnRef={TxnRef}, ResponseCode={ResponseCode}",
                txnRef, validation.ResponseCode);

            return new VnPayIpnProcessingResult
            {
                IpnResponse = VnPayIpnResponse.Success(),
                Failure = new VnPayFailureInfo
                {
                    UserId = transaction.UserId,
                    Amount = transaction.Amount,
                    ReferenceCode = transaction.ReferenceCode,
                    ResponseCode = validation.ResponseCode
                }
            };
        }
    }
}
