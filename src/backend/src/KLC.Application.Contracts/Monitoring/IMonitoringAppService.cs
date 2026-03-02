using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace KLC.Monitoring;

public interface IMonitoringAppService : IApplicationService
{
    Task<DashboardDto> GetDashboardAsync();

    Task<PagedResultDto<StatusChangeLogDto>> GetStatusHistoryAsync(Guid stationId, GetStatusHistoryDto input);

    Task<EnergySummaryDto> GetStationEnergySummaryAsync(Guid stationId, GetEnergySummaryDto input);

    Task<EnergySummaryDto> GetConnectorEnergySummaryAsync(Guid connectorId, GetEnergySummaryDto input);
}
