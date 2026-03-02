using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Permissions;
using KLC.Stations;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace KLC.Faults;

[Authorize(KLCPermissions.Faults.Default)]
public class FaultAppService : KLCAppService, IFaultAppService
{
    private readonly IRepository<Fault, Guid> _faultRepository;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;

    public FaultAppService(
        IRepository<Fault, Guid> faultRepository,
        IRepository<ChargingStation, Guid> stationRepository)
    {
        _faultRepository = faultRepository;
        _stationRepository = stationRepository;
    }

    public async Task<FaultDto> GetAsync(Guid id)
    {
        var fault = await _faultRepository.GetAsync(id);
        var station = await _stationRepository.GetAsync(fault.StationId);

        return MapToDto(fault, station.Name);
    }

    public async Task<PagedResultDto<FaultListDto>> GetListAsync(GetFaultListDto input)
    {
        return await GetFaultsInternalAsync(input, null);
    }

    public async Task<PagedResultDto<FaultListDto>> GetByStationAsync(Guid stationId, GetFaultListDto input)
    {
        return await GetFaultsInternalAsync(input, stationId);
    }

    [Authorize(KLCPermissions.Faults.Update)]
    public async Task<FaultDto> UpdateStatusAsync(Guid id, UpdateFaultStatusDto input)
    {
        var fault = await _faultRepository.GetAsync(id);

        // Validate status transition
        if (fault.Status == FaultStatus.Resolved && input.Status != FaultStatus.Resolved)
        {
            throw new BusinessException("MOD_005_002")
                .WithData("currentStatus", fault.Status)
                .WithData("newStatus", input.Status);
        }

        fault.UpdateStatus(input.Status, input.ResolutionNotes);
        await _faultRepository.UpdateAsync(fault);

        var station = await _stationRepository.GetAsync(fault.StationId);
        return MapToDto(fault, station.Name);
    }

    private async Task<PagedResultDto<FaultListDto>> GetFaultsInternalAsync(GetFaultListDto input, Guid? stationId)
    {
        var query = await _faultRepository.GetQueryableAsync();

        if (stationId.HasValue)
        {
            query = query.Where(f => f.StationId == stationId.Value);
        }
        else if (input.StationId.HasValue)
        {
            query = query.Where(f => f.StationId == input.StationId.Value);
        }

        if (input.Status.HasValue)
        {
            query = query.Where(f => f.Status == input.Status.Value);
        }

        if (input.FromDate.HasValue)
        {
            query = query.Where(f => f.DetectedAt >= input.FromDate.Value);
        }

        if (input.ToDate.HasValue)
        {
            query = query.Where(f => f.DetectedAt <= input.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.ToLower();
            query = query.Where(f => f.ErrorCode.ToLower().Contains(search) ||
                                     (f.ErrorInfo != null && f.ErrorInfo.ToLower().Contains(search)));
        }

        if (input.Cursor.HasValue)
        {
            query = query.Where(f => f.Id.CompareTo(input.Cursor.Value) > 0);
        }

        query = query.OrderByDescending(f => f.DetectedAt);

        var totalCount = await AsyncExecuter.CountAsync(query);
        var faults = await AsyncExecuter.ToListAsync(query.Take(input.MaxResultCount));

        var stationIds = faults.Select(f => f.StationId).Distinct().ToList();
        var stations = await _stationRepository.GetListAsync(st => stationIds.Contains(st.Id));
        var stationMap = stations.ToDictionary(st => st.Id, st => st.Name);

        var dtos = faults.Select(f => new FaultListDto
        {
            Id = f.Id,
            StationName = stationMap.TryGetValue(f.StationId, out var name) ? name : "Unknown",
            ConnectorNumber = f.ConnectorNumber,
            ErrorCode = f.ErrorCode,
            Status = f.Status,
            DetectedAt = f.DetectedAt
        }).ToList();

        return new PagedResultDto<FaultListDto>(totalCount, dtos);
    }

    private static FaultDto MapToDto(Fault fault, string stationName)
    {
        return new FaultDto
        {
            Id = fault.Id,
            StationId = fault.StationId,
            StationName = stationName,
            ConnectorNumber = fault.ConnectorNumber,
            ErrorCode = fault.ErrorCode,
            ErrorInfo = fault.ErrorInfo,
            VendorErrorCode = fault.VendorErrorCode,
            Priority = fault.Priority,
            Status = fault.Status,
            DetectedAt = fault.DetectedAt,
            ResolvedAt = fault.ResolvedAt,
            ResolutionNotes = fault.ResolutionNotes,
            CreationTime = fault.CreationTime,
            CreatorId = fault.CreatorId,
            LastModificationTime = fault.LastModificationTime,
            LastModifierId = fault.LastModifierId
        };
    }
}
