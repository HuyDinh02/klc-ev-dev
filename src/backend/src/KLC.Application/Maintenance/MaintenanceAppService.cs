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

namespace KLC.Maintenance;

[Authorize(KLCPermissions.Maintenance.Default)]
public class MaintenanceAppService : KLCAppService, IMaintenanceAppService
{
    private readonly IRepository<MaintenanceTask, Guid> _taskRepository;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;

    public MaintenanceAppService(
        IRepository<MaintenanceTask, Guid> taskRepository,
        IRepository<ChargingStation, Guid> stationRepository)
    {
        _taskRepository = taskRepository;
        _stationRepository = stationRepository;
    }

    public async Task<PagedResultDto<MaintenanceTaskDto>> GetListAsync(GetMaintenanceTaskListDto input)
    {
        var query = await _taskRepository.GetQueryableAsync();

        if (input.Status.HasValue)
            query = query.Where(t => t.Status == input.Status.Value);

        if (input.Type.HasValue)
            query = query.Where(t => t.Type == input.Type.Value);

        if (input.StationId.HasValue)
            query = query.Where(t => t.StationId == input.StationId.Value);

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = query.OrderByDescending(t => t.CreationTime);
        var tasks = await AsyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));

        var stationIds = tasks.Select(t => t.StationId).Distinct().ToList();
        var stations = await AsyncExecuter.ToListAsync(
            (await _stationRepository.GetQueryableAsync()).Where(s => stationIds.Contains(s.Id)));
        var stationMap = stations.ToDictionary(s => s.Id, s => s.Name);

        var dtos = tasks.Select(t => MapToDto(t, stationMap)).ToList();
        return new PagedResultDto<MaintenanceTaskDto>(totalCount, dtos);
    }

    public async Task<MaintenanceTaskDto> GetAsync(Guid id)
    {
        var task = await _taskRepository.GetAsync(id);
        var station = await _stationRepository.GetAsync(task.StationId);
        return MapToDto(task, new() { { station.Id, station.Name } });
    }

    public async Task<MaintenanceStatsDto> GetStatsAsync()
    {
        var tasks = await _taskRepository.GetListAsync();
        return new MaintenanceStatsDto
        {
            PlannedCount = tasks.Count(t => t.Status == MaintenanceTaskStatus.Planned),
            InProgressCount = tasks.Count(t => t.Status == MaintenanceTaskStatus.InProgress),
            CompletedCount = tasks.Count(t => t.Status == MaintenanceTaskStatus.Completed),
            OverdueCount = tasks.Count(t => t.IsOverdue())
        };
    }

    [Authorize(KLCPermissions.Maintenance.Create)]
    public async Task<MaintenanceTaskDto> CreateAsync(CreateMaintenanceTaskDto input)
    {
        var station = await _stationRepository.FindAsync(input.StationId);
        if (station == null)
            throw new BusinessException(KLCDomainErrorCodes.Station.NotFound);

        var task = new MaintenanceTask(
            GuidGenerator.Create(),
            input.StationId,
            input.Type,
            input.Title,
            input.AssignedTo,
            input.ScheduledDate,
            input.ConnectorNumber,
            input.Description);

        await _taskRepository.InsertAsync(task);
        return MapToDto(task, new() { { station.Id, station.Name } });
    }

    [Authorize(KLCPermissions.Maintenance.Update)]
    public async Task<MaintenanceTaskDto> UpdateAsync(Guid id, UpdateMaintenanceTaskDto input)
    {
        var task = await _taskRepository.GetAsync(id);
        task.Update(input.Title, input.Description, input.AssignedTo, input.ScheduledDate);
        await _taskRepository.UpdateAsync(task);

        var station = await _stationRepository.GetAsync(task.StationId);
        return MapToDto(task, new() { { station.Id, station.Name } });
    }

    [Authorize(KLCPermissions.Maintenance.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await _taskRepository.DeleteAsync(id);
    }

    [Authorize(KLCPermissions.Maintenance.Update)]
    public async Task<MaintenanceTaskDto> StartAsync(Guid id)
    {
        var task = await _taskRepository.GetAsync(id);
        task.Start();
        await _taskRepository.UpdateAsync(task);

        var station = await _stationRepository.GetAsync(task.StationId);
        return MapToDto(task, new() { { station.Id, station.Name } });
    }

    [Authorize(KLCPermissions.Maintenance.Update)]
    public async Task<MaintenanceTaskDto> CompleteAsync(Guid id, CompleteMaintenanceTaskDto input)
    {
        var task = await _taskRepository.GetAsync(id);
        task.Complete(input.Notes);
        await _taskRepository.UpdateAsync(task);

        var station = await _stationRepository.GetAsync(task.StationId);
        return MapToDto(task, new() { { station.Id, station.Name } });
    }

    [Authorize(KLCPermissions.Maintenance.Update)]
    public async Task<MaintenanceTaskDto> CancelAsync(Guid id, CancelMaintenanceTaskDto input)
    {
        var task = await _taskRepository.GetAsync(id);
        task.Cancel(input.Notes);
        await _taskRepository.UpdateAsync(task);

        var station = await _stationRepository.GetAsync(task.StationId);
        return MapToDto(task, new() { { station.Id, station.Name } });
    }

    private static MaintenanceTaskDto MapToDto(MaintenanceTask task, System.Collections.Generic.Dictionary<Guid, string> stationMap)
    {
        return new MaintenanceTaskDto
        {
            Id = task.Id,
            StationId = task.StationId,
            StationName = stationMap.TryGetValue(task.StationId, out var name) ? name : "Unknown",
            ConnectorNumber = task.ConnectorNumber,
            Type = task.Type,
            Status = task.Status,
            Title = task.Title,
            Description = task.Description,
            AssignedTo = task.AssignedTo,
            ScheduledDate = task.ScheduledDate,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt,
            Notes = task.Notes,
            CreationTime = task.CreationTime
        };
    }
}
