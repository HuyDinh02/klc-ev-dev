using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Notifications;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Controllers.Notifications;

[ApiController]
[Route("api/v1/admin/notifications")]
[Authorize(KLCPermissions.Notifications.Default)]
public class BroadcastController : KLCController
{
    private readonly IBroadcastAppService _broadcastAppService;

    public BroadcastController(IBroadcastAppService broadcastAppService)
    {
        _broadcastAppService = broadcastAppService;
    }

    [HttpPost("broadcast")]
    [Authorize(KLCPermissions.Notifications.Broadcast)]
    public async Task<ActionResult<BroadcastResultDto>> BroadcastAsync(
        [FromBody] BroadcastNotificationDto input)
    {
        var result = await _broadcastAppService.BroadcastAsync(input);
        return Ok(result);
    }

    [HttpGet("broadcasts")]
    public async Task<ActionResult<List<BroadcastHistoryDto>>> GetBroadcastHistoryAsync(
        [FromQuery] GetBroadcastHistoryDto input)
    {
        var result = await _broadcastAppService.GetBroadcastHistoryAsync(input);
        return Ok(result);
    }
}
