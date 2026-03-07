using System.Threading.Tasks;
using KLC.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Controllers.Users;

[ApiController]
[Route("api/v1/profile")]
[Authorize]
public class ProfileController : KLCController
{
    private readonly IUserProfileAppService _userProfileAppService;

    public ProfileController(IUserProfileAppService userProfileAppService)
    {
        _userProfileAppService = userProfileAppService;
    }

    [HttpGet]
    public async Task<ActionResult<UserProfileDto>> GetProfileAsync()
    {
        var result = await _userProfileAppService.GetProfileAsync();
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<UserProfileDto>> UpdateProfileAsync([FromBody] UpdateProfileDto input)
    {
        var result = await _userProfileAppService.UpdateProfileAsync(input);
        return Ok(result);
    }

    [HttpPost("phone")]
    public async Task<ActionResult> UpdatePhoneAsync([FromBody] UpdatePhoneDto input)
    {
        await _userProfileAppService.UpdatePhoneAsync(input);
        return NoContent();
    }

    [HttpPost("phone/verify")]
    public async Task<ActionResult> VerifyPhoneAsync([FromBody] VerifyPhoneDto input)
    {
        await _userProfileAppService.VerifyPhoneAsync(input);
        return NoContent();
    }

    [HttpPost("email")]
    public async Task<ActionResult> UpdateEmailAsync([FromBody] UpdateEmailDto input)
    {
        await _userProfileAppService.UpdateEmailAsync(input);
        return NoContent();
    }

    [HttpPost("email/verify")]
    public async Task<ActionResult> VerifyEmailAsync([FromBody] VerifyEmailDto input)
    {
        await _userProfileAppService.VerifyEmailAsync(input);
        return NoContent();
    }

    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePasswordAsync([FromBody] ChangePasswordDto input)
    {
        await _userProfileAppService.ChangePasswordAsync(input);
        return NoContent();
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<UserStatisticsDto>> GetStatisticsAsync()
    {
        var result = await _userProfileAppService.GetStatisticsAsync();
        return Ok(result);
    }

    [HttpPost("deactivate")]
    public async Task<ActionResult> DeactivateAccountAsync()
    {
        await _userProfileAppService.DeactivateAccountAsync();
        return NoContent();
    }
}
