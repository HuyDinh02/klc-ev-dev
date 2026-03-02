using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace KLC.Sessions;

public interface ISessionAppService : IApplicationService
{
    Task<ChargingSessionDto> StartAsync(StartSessionDto input);
    Task<ChargingSessionDto> StopAsync(Guid id, StopSessionDto? input = null);
    Task<ChargingSessionDto> GetAsync(Guid id);
    Task<ActiveSessionDto?> GetActiveSessionAsync();
    Task<PagedResultDto<SessionListDto>> GetHistoryAsync(GetSessionListDto input);
    Task<List<MeterValueDto>> GetMeterValuesAsync(Guid sessionId);

    // Admin endpoints
    Task<PagedResultDto<SessionListDto>> GetAllSessionsAsync(GetSessionListDto input);
}
