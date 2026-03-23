using System.Threading.Tasks;

namespace KLC.Ocpp;

public interface IOcppCommandService
{
    Task<OcppCommandResultDto> RemoteStartAsync(RemoteStartRequestDto request);
    Task<OcppCommandResultDto> RemoteStopAsync(RemoteStopRequestDto request);
    Task<OcppCommandResultDto> ResetAsync(ResetRequestDto request);
    Task<OcppCommandResultDto> UnlockConnectorAsync(UnlockConnectorRequestDto request);
    Task<OcppCommandResultDto> ChangeAvailabilityAsync(ChangeAvailabilityRequestDto request);
    Task<OcppCommandResultDto> GetConfigurationAsync(GetConfigurationRequestDto request);
    Task<OcppCommandResultDto> ChangeConfigurationAsync(ChangeConfigurationRequestDto request);
    Task<OcppCommandResultDto> TriggerMessageAsync(TriggerMessageRequestDto request);
}
