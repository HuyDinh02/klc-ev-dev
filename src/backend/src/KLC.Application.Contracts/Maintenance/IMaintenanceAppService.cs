using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace KLC.Maintenance;

public interface IMaintenanceAppService : IApplicationService
{
    Task<PagedResultDto<MaintenanceTaskDto>> GetListAsync(GetMaintenanceTaskListDto input);
    Task<MaintenanceTaskDto> GetAsync(Guid id);
    Task<MaintenanceStatsDto> GetStatsAsync();
    Task<MaintenanceTaskDto> CreateAsync(CreateMaintenanceTaskDto input);
    Task<MaintenanceTaskDto> UpdateAsync(Guid id, UpdateMaintenanceTaskDto input);
    Task DeleteAsync(Guid id);
    Task<MaintenanceTaskDto> StartAsync(Guid id);
    Task<MaintenanceTaskDto> CompleteAsync(Guid id, CompleteMaintenanceTaskDto input);
    Task<MaintenanceTaskDto> CancelAsync(Guid id, CancelMaintenanceTaskDto input);
}
