using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace KLC.Stations;

/// <summary>
/// Application service interface for station management.
/// </summary>
public interface IStationAppService : IApplicationService
{
    /// <summary>
    /// Creates a new charging station.
    /// </summary>
    Task<StationDto> CreateAsync(CreateStationDto input);

    /// <summary>
    /// Updates an existing charging station.
    /// </summary>
    Task<StationDto> UpdateAsync(Guid id, UpdateStationDto input);

    /// <summary>
    /// Gets a station by ID with all connectors.
    /// </summary>
    Task<StationDto> GetAsync(Guid id);

    /// <summary>
    /// Gets a paginated list of stations.
    /// </summary>
    Task<PagedResultDto<StationListDto>> GetListAsync(GetStationListDto input);

    /// <summary>
    /// Decommissions a station (soft delete, preserves history).
    /// </summary>
    Task DecommissionAsync(Guid id);

    /// <summary>
    /// Enables a station.
    /// </summary>
    Task EnableAsync(Guid id);

    /// <summary>
    /// Disables a station.
    /// </summary>
    Task DisableAsync(Guid id);

    // Amenities
    Task<List<StationAmenityDto>> GetAmenitiesAsync(Guid stationId);
    Task<StationAmenityDto> AddAmenityAsync(Guid stationId, AddStationAmenityDto input);
    Task RemoveAmenityAsync(Guid stationId, Guid amenityId);

    // Photos
    Task<List<StationPhotoDto>> GetPhotosAsync(Guid stationId);
    Task<StationPhotoDto> AddPhotoAsync(Guid stationId, AddStationPhotoDto input);
    Task RemovePhotoAsync(Guid stationId, Guid photoId);
    Task SetPrimaryPhotoAsync(Guid stationId, Guid photoId);

    // Photo upload
    Task<StationPhotoUploadResultDto> UploadPhotoAsync(System.IO.Stream stream, string fileName);
}
