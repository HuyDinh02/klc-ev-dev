using System;
using System.Threading.Tasks;
using KLC.Marketing;
using KLC.MobileUsers;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Controllers.Marketing;

[ApiController]
[Route("api/v1/admin/vouchers")]
[Authorize(KLCPermissions.Vouchers.Default)]
public class VoucherAdminController : KLCController
{
    private readonly IVoucherAppService _voucherAppService;

    public VoucherAdminController(IVoucherAppService voucherAppService)
    {
        _voucherAppService = voucherAppService;
    }

    [HttpGet]
    public async Task<ActionResult<CursorPagedResultDto<VoucherListDto>>> GetVouchersAsync(
        [FromQuery] GetVoucherListDto input)
    {
        var result = await _voucherAppService.GetListAsync(input);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VoucherDetailDto>> GetVoucherAsync(Guid id)
    {
        var result = await _voucherAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(KLCPermissions.Vouchers.Create)]
    public async Task<ActionResult<CreateVoucherResultDto>> CreateVoucherAsync(
        [FromBody] CreateVoucherDto input)
    {
        var result = await _voucherAppService.CreateAsync(input);
        return CreatedAtAction(nameof(GetVoucherAsync), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(KLCPermissions.Vouchers.Update)]
    public async Task<ActionResult> UpdateVoucherAsync(Guid id, [FromBody] UpdateVoucherDto input)
    {
        await _voucherAppService.UpdateAsync(id, input);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(KLCPermissions.Vouchers.Delete)]
    public async Task<ActionResult> DeleteVoucherAsync(Guid id)
    {
        await _voucherAppService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id:guid}/usage")]
    public async Task<ActionResult<VoucherUsageResultDto>> GetVoucherUsageAsync(Guid id)
    {
        var result = await _voucherAppService.GetUsageAsync(id);
        return Ok(result);
    }
}

[ApiController]
[Route("api/v1/admin/promotions")]
[Authorize(KLCPermissions.Promotions.Default)]
public class PromotionAdminController : KLCController
{
    private readonly IPromotionAppService _promotionAppService;

    public PromotionAdminController(IPromotionAppService promotionAppService)
    {
        _promotionAppService = promotionAppService;
    }

    [HttpGet]
    public async Task<ActionResult<CursorPagedResultDto<PromotionListDto>>> GetPromotionsAsync(
        [FromQuery] GetPromotionListDto input)
    {
        var result = await _promotionAppService.GetListAsync(input);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PromotionDetailDto>> GetPromotionAsync(Guid id)
    {
        var result = await _promotionAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(KLCPermissions.Promotions.Create)]
    public async Task<ActionResult<CreatePromotionResultDto>> CreatePromotionAsync(
        [FromBody] CreatePromotionDto input)
    {
        var result = await _promotionAppService.CreateAsync(input);
        return CreatedAtAction(nameof(GetPromotionAsync), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(KLCPermissions.Promotions.Update)]
    public async Task<ActionResult> UpdatePromotionAsync(Guid id, [FromBody] UpdatePromotionDto input)
    {
        await _promotionAppService.UpdateAsync(id, input);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(KLCPermissions.Promotions.Delete)]
    public async Task<ActionResult> DeletePromotionAsync(Guid id)
    {
        await _promotionAppService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("upload-image")]
    [Authorize(KLCPermissions.Promotions.Create)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ImageUploadResultDto>> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = new { message = "No file provided" } });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = new { message = "File size must be less than 5MB" } });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!Array.Exists(allowedTypes, t => t == file.ContentType))
            return BadRequest(new { error = new { message = "Only JPEG, PNG, GIF, and WebP images are allowed" } });

        using var stream = file.OpenReadStream();
        var result = await _promotionAppService.UploadImageAsync(stream, file.FileName);
        return Ok(result);
    }
}
