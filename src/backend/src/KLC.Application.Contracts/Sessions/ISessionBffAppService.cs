using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace KLC.Sessions;

/// <summary>
/// Application service encapsulating session start/stop business logic.
/// The BFF delegates to this service for all session mutations.
/// Shared between Admin API and Driver BFF.
/// </summary>
public interface ISessionBffAppService : IApplicationService
{
    /// <summary>
    /// Start a charging session: resolve connector, validate availability/wallet/fleet policy,
    /// create session entity, send RemoteStart to OCPP Gateway.
    /// </summary>
    Task<StartSessionResultDto> StartSessionAsync(StartSessionInput input);

    /// <summary>
    /// Stop a charging session: validate ownership/status, mark stopping,
    /// send RemoteStop to OCPP Gateway.
    /// </summary>
    Task<StopSessionResultDto> StopSessionAsync(Guid userId, Guid sessionId);
}
