using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Permissions;
using KLC.Stations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace KLC.Controllers.Stations;

[ApiController]
[Route("api/v1/stations")]
[Authorize(KLCPermissions.Stations.Default)]
public class StationController : KLCController
{
    private readonly IStationAppService _stationAppService;

    public StationController(IStationAppService stationAppService)
    {
        _stationAppService = stationAppService;
    }

    /// <summary>
    /// Creates a new charging station.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<StationDto>> CreateAsync([FromBody] CreateStationDto input)
    {
        var result = await _stationAppService.CreateAsync(input);
        return CreatedAtAction(nameof(GetAsync), new { id = result.Id }, result);
    }

    /// <summary>
    /// Gets a station by ID with all connectors.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StationDto>> GetAsync(Guid id)
    {
        var result = await _stationAppService.GetAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Gets a paginated list of stations.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<StationListDto>>> GetListAsync([FromQuery] GetStationListDto input)
    {
        var result = await _stationAppService.GetListAsync(input);
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing charging station.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StationDto>> UpdateAsync(Guid id, [FromBody] UpdateStationDto input)
    {
        var result = await _stationAppService.UpdateAsync(id, input);
        return Ok(result);
    }

    /// <summary>
    /// Decommissions a station (preserves history).
    /// </summary>
    [HttpPost("{id:guid}/decommission")]
    public async Task<ActionResult> DecommissionAsync(Guid id)
    {
        await _stationAppService.DecommissionAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Enables a station.
    /// </summary>
    [HttpPost("{id:guid}/enable")]
    public async Task<ActionResult> EnableAsync(Guid id)
    {
        await _stationAppService.EnableAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Disables a station.
    /// </summary>
    [HttpPost("{id:guid}/disable")]
    public async Task<ActionResult> DisableAsync(Guid id)
    {
        await _stationAppService.DisableAsync(id);
        return NoContent();
    }

    // --- Amenities ---

    [HttpGet("{stationId:guid}/amenities")]
    public async Task<ActionResult<List<StationAmenityDto>>> GetAmenitiesAsync(Guid stationId)
    {
        var result = await _stationAppService.GetAmenitiesAsync(stationId);
        return Ok(result);
    }

    [HttpPost("{stationId:guid}/amenities")]
    public async Task<ActionResult<StationAmenityDto>> AddAmenityAsync(Guid stationId, [FromBody] AddStationAmenityDto input)
    {
        var result = await _stationAppService.AddAmenityAsync(stationId, input);
        return Created($"/api/v1/stations/{stationId}/amenities/{result.Id}", result);
    }

    [HttpDelete("{stationId:guid}/amenities/{amenityId:guid}")]
    public async Task<ActionResult> RemoveAmenityAsync(Guid stationId, Guid amenityId)
    {
        await _stationAppService.RemoveAmenityAsync(stationId, amenityId);
        return NoContent();
    }

    // --- Photos ---

    [HttpGet("{stationId:guid}/photos")]
    public async Task<ActionResult<List<StationPhotoDto>>> GetPhotosAsync(Guid stationId)
    {
        var result = await _stationAppService.GetPhotosAsync(stationId);
        return Ok(result);
    }

    [HttpPost("{stationId:guid}/photos")]
    public async Task<ActionResult<StationPhotoDto>> AddPhotoAsync(Guid stationId, [FromBody] AddStationPhotoDto input)
    {
        var result = await _stationAppService.AddPhotoAsync(stationId, input);
        return Created($"/api/v1/stations/{stationId}/photos/{result.Id}", result);
    }

    [HttpDelete("{stationId:guid}/photos/{photoId:guid}")]
    public async Task<ActionResult> RemovePhotoAsync(Guid stationId, Guid photoId)
    {
        await _stationAppService.RemovePhotoAsync(stationId, photoId);
        return NoContent();
    }

    [HttpPost("{stationId:guid}/photos/{photoId:guid}/set-primary")]
    public async Task<ActionResult> SetPrimaryPhotoAsync(Guid stationId, Guid photoId)
    {
        await _stationAppService.SetPrimaryPhotoAsync(stationId, photoId);
        return NoContent();
    }

    [HttpPost("upload-photo")]
    [Authorize(KLCPermissions.Stations.Update)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<StationPhotoUploadResultDto>> UploadPhotoAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = new { message = "No file provided" } });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = new { message = "File size must be less than 5MB" } });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!Array.Exists(allowedTypes, t => t == file.ContentType))
            return BadRequest(new { error = new { message = "Only JPEG, PNG, GIF, and WebP images are allowed" } });

        using var stream = file.OpenReadStream();
        var result = await _stationAppService.UploadPhotoAsync(stream, file.FileName);
        return Ok(result);
    }
}
