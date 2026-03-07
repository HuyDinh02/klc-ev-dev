using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Permissions;
using KLC.Sessions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace KLC.Stations;

[Authorize(KLCPermissions.Stations.Default)]
public class StationAppService : KLCAppService, IStationAppService
{
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<StationAmenity, Guid> _amenityRepository;
    private readonly IRepository<StationPhoto, Guid> _photoRepository;
    private readonly StationMapper _mapper;

    public StationAppService(
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<StationAmenity, Guid> amenityRepository,
        IRepository<StationPhoto, Guid> photoRepository)
    {
        _stationRepository = stationRepository;
        _sessionRepository = sessionRepository;
        _amenityRepository = amenityRepository;
        _photoRepository = photoRepository;
        _mapper = new StationMapper();
    }

    [Authorize(KLCPermissions.Stations.Create)]
    public async Task<StationDto> CreateAsync(CreateStationDto input)
    {
        // Check for duplicate station code
        var existingStation = await _stationRepository.FirstOrDefaultAsync(s => s.StationCode == input.StationCode);
        if (existingStation != null)
        {
            throw new BusinessException(KLCDomainErrorCodes.Station.DuplicateCode)
                .WithData("stationCode", input.StationCode);
        }

        var station = new ChargingStation(
            GuidGenerator.Create(),
            input.StationCode,
            input.Name,
            input.Address,
            input.Latitude,
            input.Longitude,
            input.StationGroupId,
            input.TariffPlanId
        );

        await _stationRepository.InsertAsync(station);

        return _mapper.ToDto(station);
    }

    [Authorize(KLCPermissions.Stations.Update)]
    public async Task<StationDto> UpdateAsync(Guid id, UpdateStationDto input)
    {
        var station = await _stationRepository.GetAsync(id);

        station.SetName(input.Name);
        station.SetAddress(input.Address);
        station.SetLocation(input.Latitude, input.Longitude);
        station.SetStationGroup(input.StationGroupId);
        station.SetTariffPlan(input.TariffPlanId);

        await _stationRepository.UpdateAsync(station);

        return _mapper.ToDto(station);
    }

    public async Task<StationDto> GetAsync(Guid id)
    {
        var query = await _stationRepository.WithDetailsAsync(s => s.Connectors);
        var station = await AsyncExecuter.FirstOrDefaultAsync(query.Where(s => s.Id == id));

        if (station == null)
        {
            throw new BusinessException(KLCDomainErrorCodes.Station.NotFound);
        }

        return _mapper.ToDto(station);
    }

    public async Task<PagedResultDto<StationListDto>> GetListAsync(GetStationListDto input)
    {
        var query = await _stationRepository.WithDetailsAsync(s => s.Connectors);

        // Apply filters
        if (input.Status.HasValue)
        {
            query = query.Where(s => s.Status == input.Status.Value);
        }

        if (input.StationGroupId.HasValue)
        {
            query = query.Where(s => s.StationGroupId == input.StationGroupId.Value);
        }

        if (input.IsEnabled.HasValue)
        {
            query = query.Where(s => s.IsEnabled == input.IsEnabled.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(search) ||
                s.StationCode.ToLower().Contains(search));
        }

        // Cursor-based pagination
        if (input.Cursor.HasValue)
        {
            query = query.Where(s => s.Id.CompareTo(input.Cursor.Value) > 0);
        }

        // Apply sorting
        query = input.SortBy?.ToLower() switch
        {
            "name" => input.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(s => s.Name)
                : query.OrderBy(s => s.Name),
            "stationcode" => input.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(s => s.StationCode)
                : query.OrderBy(s => s.StationCode),
            "status" => input.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(s => s.Status)
                : query.OrderBy(s => s.Status),
            _ => query.OrderBy(s => s.Id)
        };

        var totalCount = await AsyncExecuter.CountAsync(query);
        var stations = await AsyncExecuter.ToListAsync(query.Take(input.MaxResultCount));

        var dtos = stations.Select(s =>
        {
            var dto = _mapper.ToListDto(s);
            dto.ConnectorCount = s.Connectors.Count;
            return dto;
        }).ToList();

        return new PagedResultDto<StationListDto>(totalCount, dtos);
    }

    [Authorize(KLCPermissions.Stations.Decommission)]
    public async Task DecommissionAsync(Guid id)
    {
        var query = await _stationRepository.WithDetailsAsync(s => s.Connectors);
        var station = await AsyncExecuter.FirstOrDefaultAsync(query.Where(s => s.Id == id));

        if (station == null)
        {
            throw new BusinessException(KLCDomainErrorCodes.Station.NotFound);
        }

        // MOD-001-003: Cannot decommission station with active sessions
        var hasActiveSessions = await _sessionRepository.AnyAsync(s =>
            s.StationId == id &&
            (s.Status == SessionStatus.Pending ||
             s.Status == SessionStatus.Starting ||
             s.Status == SessionStatus.InProgress ||
             s.Status == SessionStatus.Suspended ||
             s.Status == SessionStatus.Stopping));

        if (hasActiveSessions)
        {
            throw new BusinessException(KLCDomainErrorCodes.Station.HasActiveSessions)
                .WithData("stationId", id);
        }

        station.UpdateStatus(StationStatus.Decommissioned);
        station.Disable();

        await _stationRepository.UpdateAsync(station);
    }

    public async Task EnableAsync(Guid id)
    {
        var station = await _stationRepository.GetAsync(id);
        station.Enable();
        await _stationRepository.UpdateAsync(station);
    }

    public async Task DisableAsync(Guid id)
    {
        var station = await _stationRepository.GetAsync(id);
        station.Disable();
        await _stationRepository.UpdateAsync(station);
    }

    // --- Amenities ---

    public async Task<List<StationAmenityDto>> GetAmenitiesAsync(Guid stationId)
    {
        var amenities = await _amenityRepository.GetListAsync(a => a.StationId == stationId);
        return amenities.Select(a => new StationAmenityDto
        {
            Id = a.Id,
            StationId = a.StationId,
            AmenityType = a.AmenityType
        }).ToList();
    }

    [Authorize(KLCPermissions.Stations.Update)]
    public async Task<StationAmenityDto> AddAmenityAsync(Guid stationId, AddStationAmenityDto input)
    {
        // Verify station exists
        await _stationRepository.GetAsync(stationId);

        // Check for duplicate
        var existing = await _amenityRepository.FirstOrDefaultAsync(
            a => a.StationId == stationId && a.AmenityType == input.AmenityType);
        if (existing != null)
        {
            return new StationAmenityDto
            {
                Id = existing.Id,
                StationId = existing.StationId,
                AmenityType = existing.AmenityType
            };
        }

        var amenity = new StationAmenity(GuidGenerator.Create(), stationId, input.AmenityType);
        await _amenityRepository.InsertAsync(amenity);

        return new StationAmenityDto
        {
            Id = amenity.Id,
            StationId = amenity.StationId,
            AmenityType = amenity.AmenityType
        };
    }

    [Authorize(KLCPermissions.Stations.Update)]
    public async Task RemoveAmenityAsync(Guid stationId, Guid amenityId)
    {
        var amenity = await _amenityRepository.GetAsync(amenityId);
        if (amenity.StationId != stationId)
        {
            throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);
        }
        await _amenityRepository.DeleteAsync(amenity);
    }

    // --- Photos ---

    public async Task<List<StationPhotoDto>> GetPhotosAsync(Guid stationId)
    {
        var photos = await _photoRepository.GetListAsync(p => p.StationId == stationId);
        return photos.OrderBy(p => p.SortOrder).Select(p => new StationPhotoDto
        {
            Id = p.Id,
            StationId = p.StationId,
            Url = p.Url,
            ThumbnailUrl = p.ThumbnailUrl,
            IsPrimary = p.IsPrimary,
            SortOrder = p.SortOrder,
            CreationTime = p.CreationTime
        }).ToList();
    }

    [Authorize(KLCPermissions.Stations.Update)]
    public async Task<StationPhotoDto> AddPhotoAsync(Guid stationId, AddStationPhotoDto input)
    {
        await _stationRepository.GetAsync(stationId);

        var photo = new StationPhoto(
            GuidGenerator.Create(),
            stationId,
            input.Url,
            input.ThumbnailUrl,
            input.IsPrimary,
            input.SortOrder);

        // If setting as primary, unset other primaries
        if (input.IsPrimary)
        {
            var existing = await _photoRepository.GetListAsync(p => p.StationId == stationId && p.IsPrimary);
            foreach (var p in existing) p.UnsetPrimary();
            if (existing.Any()) await _photoRepository.UpdateManyAsync(existing);
        }

        await _photoRepository.InsertAsync(photo);

        return new StationPhotoDto
        {
            Id = photo.Id,
            StationId = photo.StationId,
            Url = photo.Url,
            ThumbnailUrl = photo.ThumbnailUrl,
            IsPrimary = photo.IsPrimary,
            SortOrder = photo.SortOrder,
            CreationTime = photo.CreationTime
        };
    }

    [Authorize(KLCPermissions.Stations.Update)]
    public async Task RemovePhotoAsync(Guid stationId, Guid photoId)
    {
        var photo = await _photoRepository.GetAsync(photoId);
        if (photo.StationId != stationId)
        {
            throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);
        }
        await _photoRepository.DeleteAsync(photo);
    }

    [Authorize(KLCPermissions.Stations.Update)]
    public async Task SetPrimaryPhotoAsync(Guid stationId, Guid photoId)
    {
        var photos = await _photoRepository.GetListAsync(p => p.StationId == stationId);
        foreach (var photo in photos)
        {
            if (photo.Id == photoId)
                photo.SetAsPrimary();
            else if (photo.IsPrimary)
                photo.UnsetPrimary();
        }
        await _photoRepository.UpdateManyAsync(photos);
    }
}
