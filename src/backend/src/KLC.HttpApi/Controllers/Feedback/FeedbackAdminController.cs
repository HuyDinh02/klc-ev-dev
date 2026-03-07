using System;
using System.Threading.Tasks;
using KLC.Feedback;
using KLC.MobileUsers;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Controllers.Feedback;

[ApiController]
[Route("api/v1/admin/feedback")]
[Authorize(KLCPermissions.Feedback.Default)]
public class FeedbackAdminController : KLCController
{
    private readonly IFeedbackAdminAppService _feedbackAppService;

    public FeedbackAdminController(IFeedbackAdminAppService feedbackAppService)
    {
        _feedbackAppService = feedbackAppService;
    }

    [HttpGet]
    public async Task<ActionResult<CursorPagedResultDto<FeedbackListDto>>> GetFeedbackListAsync(
        [FromQuery] GetFeedbackListDto input)
    {
        var result = await _feedbackAppService.GetListAsync(input);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FeedbackDetailDto>> GetFeedbackAsync(Guid id)
    {
        var result = await _feedbackAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpPut("{id:guid}/respond")]
    [Authorize(KLCPermissions.Feedback.Respond)]
    public async Task<ActionResult> RespondToFeedbackAsync(Guid id, [FromBody] RespondToFeedbackDto input)
    {
        await _feedbackAppService.RespondAsync(id, input);
        return NoContent();
    }

    [HttpPut("{id:guid}/close")]
    [Authorize(KLCPermissions.Feedback.Respond)]
    public async Task<ActionResult> CloseFeedbackAsync(Guid id)
    {
        await _feedbackAppService.CloseAsync(id);
        return NoContent();
    }
}
