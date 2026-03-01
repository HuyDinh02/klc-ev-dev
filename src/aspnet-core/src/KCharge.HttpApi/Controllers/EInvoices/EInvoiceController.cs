using System;
using System.Threading.Tasks;
using KCharge.EInvoices;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace KCharge.Controllers.EInvoices;

[ApiController]
[Route("api/v1/e-invoices")]
public class EInvoiceController : KChargeController
{
    private readonly IEInvoiceAppService _eInvoiceAppService;

    public EInvoiceController(IEInvoiceAppService eInvoiceAppService)
    {
        _eInvoiceAppService = eInvoiceAppService;
    }

    /// <summary>
    /// Get e-invoice details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EInvoiceDetailDto>> GetAsync(Guid id)
    {
        var result = await _eInvoiceAppService.GetAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Get e-invoice by invoice ID.
    /// </summary>
    [HttpGet("by-invoice/{invoiceId:guid}")]
    public async Task<ActionResult<EInvoiceDto>> GetByInvoiceIdAsync(Guid invoiceId)
    {
        var result = await _eInvoiceAppService.GetByInvoiceIdAsync(invoiceId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// List e-invoices with filtering (admin/finance).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<EInvoiceListDto>>> GetListAsync([FromQuery] GetEInvoiceListDto input)
    {
        var result = await _eInvoiceAppService.GetListAsync(input);
        return Ok(result);
    }

    /// <summary>
    /// Generate e-invoice for a completed invoice.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EInvoiceResultDto>> GenerateAsync([FromBody] CreateEInvoiceDto input)
    {
        var result = await _eInvoiceAppService.GenerateAsync(input);
        return CreatedAtAction(nameof(GetAsync), new { id = result.EInvoiceId }, result);
    }

    /// <summary>
    /// Retry failed e-invoice generation.
    /// </summary>
    [HttpPost("{id:guid}/retry")]
    public async Task<ActionResult<EInvoiceResultDto>> RetryAsync(Guid id)
    {
        var result = await _eInvoiceAppService.RetryAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Cancel an e-invoice (admin only).
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult> CancelAsync(Guid id)
    {
        await _eInvoiceAppService.CancelAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Get e-invoice PDF download URL.
    /// </summary>
    [HttpGet("{id:guid}/pdf-url")]
    public async Task<ActionResult<PdfUrlResponse>> GetPdfUrlAsync(Guid id)
    {
        var url = await _eInvoiceAppService.GetPdfUrlAsync(id);
        if (url == null) return NotFound();
        return Ok(new PdfUrlResponse { Url = url });
    }
}

public class PdfUrlResponse
{
    public string Url { get; set; } = string.Empty;
}
