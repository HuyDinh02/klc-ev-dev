using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace KCharge.EInvoices;

public interface IEInvoiceAppService : IApplicationService
{
    /// <summary>
    /// Get e-invoice details by ID.
    /// </summary>
    Task<EInvoiceDetailDto> GetAsync(Guid id);

    /// <summary>
    /// Get e-invoice by invoice ID.
    /// </summary>
    Task<EInvoiceDto?> GetByInvoiceIdAsync(Guid invoiceId);

    /// <summary>
    /// List e-invoices with filtering (admin/finance).
    /// </summary>
    Task<PagedResultDto<EInvoiceListDto>> GetListAsync(GetEInvoiceListDto input);

    /// <summary>
    /// Generate e-invoice for a completed invoice.
    /// </summary>
    Task<EInvoiceResultDto> GenerateAsync(CreateEInvoiceDto input);

    /// <summary>
    /// Retry failed e-invoice generation.
    /// </summary>
    Task<EInvoiceResultDto> RetryAsync(Guid id);

    /// <summary>
    /// Cancel an e-invoice (admin only).
    /// </summary>
    Task CancelAsync(Guid id);

    /// <summary>
    /// Get e-invoice PDF download URL.
    /// </summary>
    Task<string?> GetPdfUrlAsync(Guid id);
}
