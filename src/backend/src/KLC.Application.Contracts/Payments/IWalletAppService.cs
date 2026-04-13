using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace KLC.Payments;

/// <summary>
/// Application service for wallet business logic: top-up initiation, payment callback processing,
/// and wallet crediting. The BFF delegates to this service for all wallet mutations.
/// </summary>
public interface IWalletAppService : IApplicationService
{
    /// <summary>
    /// Initiate a wallet top-up: validate amount/limits, create pending transaction,
    /// call payment gateway to get redirect URL.
    /// </summary>
    Task<TopUpInitiationResultDto> InitiateTopUpAsync(InitiateTopUpInput input);

    /// <summary>
    /// Process a generic top-up callback from a payment gateway.
    /// If successful, credits the wallet and marks the transaction completed.
    /// Returns completion data so the BFF can send notifications.
    /// </summary>
    Task<TopUpCallbackResultAppDto> ProcessTopUpCallbackAsync(ProcessTopUpCallbackInput input);

    /// <summary>
    /// Process a VnPay IPN callback. Validates signature/amount, applies idempotency lock,
    /// credits wallet on success, creates in-app notification records.
    /// Returns the IPN response for VnPay + completion data for BFF notifications.
    /// </summary>
    Task<VnPayIpnProcessingResult> ProcessVnPayIpnAsync(Dictionary<string, string> queryParams);
}
