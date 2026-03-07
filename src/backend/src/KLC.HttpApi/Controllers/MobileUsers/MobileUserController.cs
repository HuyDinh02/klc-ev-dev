using System;
using System.Threading.Tasks;
using KLC.MobileUsers;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Controllers.MobileUsers;

[ApiController]
[Route("api/v1/admin/mobile-users")]
[Authorize(KLCPermissions.MobileUsers.Default)]
public class MobileUserController : KLCController
{
    private readonly IMobileUserAppService _mobileUserAppService;

    public MobileUserController(IMobileUserAppService mobileUserAppService)
    {
        _mobileUserAppService = mobileUserAppService;
    }

    [HttpGet]
    [Authorize(KLCPermissions.MobileUsers.ViewAll)]
    public async Task<ActionResult<CursorPagedResultDto<MobileUserListDto>>> GetMobileUsersAsync(
        [FromQuery] GetMobileUserListDto input)
    {
        var result = await _mobileUserAppService.GetListAsync(input);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MobileUserDetailDto>> GetMobileUserAsync(Guid id)
    {
        var result = await _mobileUserAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpPost("{id:guid}/suspend")]
    [Authorize(KLCPermissions.MobileUsers.Suspend)]
    public async Task<ActionResult> SuspendAsync(Guid id)
    {
        await _mobileUserAppService.SuspendAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/unsuspend")]
    [Authorize(KLCPermissions.MobileUsers.Suspend)]
    public async Task<ActionResult> UnsuspendAsync(Guid id)
    {
        await _mobileUserAppService.UnsuspendAsync(id);
        return NoContent();
    }

    [HttpGet("{id:guid}/sessions")]
    public async Task<ActionResult<CursorPagedResultDto<MobileUserSessionDto>>> GetUserSessionsAsync(
        Guid id, [FromQuery] GetMobileUserSessionsDto input)
    {
        var result = await _mobileUserAppService.GetSessionsAsync(id, input);
        return Ok(result);
    }

    [HttpGet("{id:guid}/transactions")]
    public async Task<ActionResult<CursorPagedResultDto<MobileUserTransactionDto>>> GetUserTransactionsAsync(
        Guid id, [FromQuery] GetMobileUserTransactionsDto input)
    {
        var result = await _mobileUserAppService.GetTransactionsAsync(id, input);
        return Ok(result);
    }

    [HttpPost("{id:guid}/wallet/adjust")]
    [Authorize(KLCPermissions.MobileUsers.WalletAdjust)]
    public async Task<ActionResult<WalletAdjustResultDto>> AdjustWalletAsync(
        Guid id, [FromBody] WalletAdjustDto input)
    {
        var result = await _mobileUserAppService.AdjustWalletAsync(id, input);
        return Ok(result);
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<MobileUserStatisticsDto>> GetStatisticsAsync()
    {
        var result = await _mobileUserAppService.GetStatisticsAsync();
        return Ok(result);
    }
}
