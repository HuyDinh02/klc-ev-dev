using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Payments;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace KLC.Controllers.Payments;

[ApiController]
[Route("api/v1/payments")]
[Authorize(KLCPermissions.Payments.Default)]
public class PaymentController : KLCController
{
    private readonly IPaymentAppService _paymentAppService;

    public PaymentController(IPaymentAppService paymentAppService)
    {
        _paymentAppService = paymentAppService;
    }

    [HttpPost("process")]
    public async Task<ActionResult<PaymentResultDto>> ProcessPaymentAsync([FromBody] ProcessPaymentDto input)
    {
        var result = await _paymentAppService.ProcessPaymentAsync(input);
        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<ActionResult<PagedResultDto<PaymentListDto>>> GetHistoryAsync([FromQuery] GetPaymentListDto input)
    {
        var result = await _paymentAppService.GetHistoryAsync(input);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentTransactionDto>> GetAsync(Guid id)
    {
        var result = await _paymentAppService.GetAsync(id);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("callback/{gateway}")]
    public async Task<ActionResult> HandleCallbackAsync(string gateway, [FromBody] PaymentCallbackDto callback)
    {
        await _paymentAppService.HandleCallbackAsync(gateway, callback);
        return Ok();
    }

    [HttpPost("{id:guid}/refund")]
    public async Task<ActionResult<RefundResultDto>> RefundAsync(Guid id, [FromBody] RefundInput input)
    {
        var result = await _paymentAppService.RefundAsync(id, input);
        return Ok(result);
    }
}

[ApiController]
[Route("api/v1/payment-methods")]
public class PaymentMethodController : KLCController
{
    private readonly IPaymentAppService _paymentAppService;

    public PaymentMethodController(IPaymentAppService paymentAppService)
    {
        _paymentAppService = paymentAppService;
    }

    [HttpPost]
    public async Task<ActionResult<PaymentMethodDto>> AddPaymentMethodAsync([FromBody] CreatePaymentMethodDto input)
    {
        var result = await _paymentAppService.AddPaymentMethodAsync(input);
        return CreatedAtAction(nameof(GetPaymentMethodsAsync), result);
    }

    [HttpGet]
    public async Task<ActionResult<List<PaymentMethodDto>>> GetPaymentMethodsAsync()
    {
        var result = await _paymentAppService.GetPaymentMethodsAsync();
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeletePaymentMethodAsync(Guid id)
    {
        await _paymentAppService.DeletePaymentMethodAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/set-default")]
    public async Task<ActionResult> SetDefaultPaymentMethodAsync(Guid id)
    {
        await _paymentAppService.SetDefaultPaymentMethodAsync(id);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/invoices")]
public class InvoiceController : KLCController
{
    private readonly IPaymentAppService _paymentAppService;

    public InvoiceController(IPaymentAppService paymentAppService)
    {
        _paymentAppService = paymentAppService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoiceAsync(Guid id)
    {
        var result = await _paymentAppService.GetInvoiceAsync(id);
        return Ok(result);
    }

    [HttpGet("by-payment/{paymentId:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoiceByPaymentAsync(Guid paymentId)
    {
        var result = await _paymentAppService.GetInvoiceByPaymentAsync(paymentId);
        if (result == null) return NotFound();
        return Ok(result);
    }
}
