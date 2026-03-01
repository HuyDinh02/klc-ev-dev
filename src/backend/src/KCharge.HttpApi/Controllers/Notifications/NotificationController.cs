using System;
using System.Threading.Tasks;
using KCharge.Notifications;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace KCharge.Controllers.Notifications;

[ApiController]
[Route("api/v1/notifications")]
public class NotificationController : KChargeController
{
    private readonly INotificationAppService _notificationAppService;

    public NotificationController(INotificationAppService notificationAppService)
    {
        _notificationAppService = notificationAppService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<NotificationListDto>>> GetMyNotificationsAsync([FromQuery] GetNotificationListDto input)
    {
        var result = await _notificationAppService.GetMyNotificationsAsync(input);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NotificationDto>> GetAsync(Guid id)
    {
        var result = await _notificationAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCountAsync()
    {
        var count = await _notificationAppService.GetUnreadCountAsync();
        return Ok(count);
    }

    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult> MarkAsReadAsync(Guid id)
    {
        await _notificationAppService.MarkAsReadAsync(id);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<ActionResult> MarkAllAsReadAsync()
    {
        await _notificationAppService.MarkAllAsReadAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/devices")]
public class DeviceController : KChargeController
{
    private readonly INotificationAppService _notificationAppService;

    public DeviceController(INotificationAppService notificationAppService)
    {
        _notificationAppService = notificationAppService;
    }

    [HttpPost("register")]
    public async Task<ActionResult> RegisterDeviceAsync([FromBody] RegisterDeviceDto input)
    {
        await _notificationAppService.RegisterDeviceAsync(input);
        return Ok();
    }
}

[ApiController]
[Route("api/v1/alerts")]
public class AlertController : KChargeController
{
    private readonly INotificationAppService _notificationAppService;

    public AlertController(INotificationAppService notificationAppService)
    {
        _notificationAppService = notificationAppService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AlertDto>>> GetAlertsAsync([FromQuery] GetAlertListDto input)
    {
        var result = await _notificationAppService.GetAlertsAsync(input);
        return Ok(result);
    }

    [HttpPost("{id:guid}/acknowledge")]
    public async Task<ActionResult> AcknowledgeAlertAsync(Guid id)
    {
        await _notificationAppService.AcknowledgeAlertAsync(id);
        return NoContent();
    }
}
