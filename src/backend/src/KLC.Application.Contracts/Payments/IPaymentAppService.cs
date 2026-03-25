using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace KLC.Payments;

public interface IPaymentAppService : IApplicationService
{
    // Payment processing
    Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto input);

    Task<PaymentTransactionDto> GetAsync(Guid id);

    Task<PagedResultDto<PaymentListDto>> GetHistoryAsync(GetPaymentListDto input);

    // Payment methods
    Task<PaymentMethodDto> AddPaymentMethodAsync(CreatePaymentMethodDto input);

    Task<List<PaymentMethodDto>> GetPaymentMethodsAsync();

    Task DeletePaymentMethodAsync(Guid id);

    Task SetDefaultPaymentMethodAsync(Guid id);

    // Invoice
    Task<InvoiceDto> GetInvoiceAsync(Guid id);

    Task<InvoiceDto?> GetInvoiceByPaymentAsync(Guid paymentId);

    // Callback (internal/webhook)
    Task HandleCallbackAsync(string gateway, PaymentCallbackDto callback);

    // VNPay IPN (GET-based callback from VNPay servers)
    Task<VnPayIpnResponse> HandleVnPayIpnAsync(Dictionary<string, string> queryParams);

    // Query VNPay transaction status (admin reconciliation)
    Task<PaymentTransactionDto> QueryVnPayTransactionAsync(Guid paymentId);

    // Refund
    Task<RefundResultDto> RefundAsync(Guid transactionId, RefundInput input);
}
