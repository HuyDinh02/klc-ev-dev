using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace KLC.Payments;

/// <summary>
/// Application service for session payment processing business logic:
/// session validation, voucher discount, gateway call, invoice generation.
/// The BFF delegates to this service for ProcessPayment mutations.
/// </summary>
public interface IPaymentProcessingAppService : IApplicationService
{
    /// <summary>
    /// Process payment for a completed charging session: validate session,
    /// apply voucher discount, call payment gateway, record voucher usage,
    /// generate invoice.
    /// </summary>
    Task<SessionPaymentResultDto> ProcessSessionPaymentAsync(ProcessSessionPaymentInput input);
}
