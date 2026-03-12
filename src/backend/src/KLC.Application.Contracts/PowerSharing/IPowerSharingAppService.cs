using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace KLC.PowerSharing;

public interface IPowerSharingAppService : IApplicationService
{
    Task<PowerSharingGroupDto> CreateAsync(CreatePowerSharingGroupDto input);
    Task<PowerSharingGroupDto> UpdateAsync(Guid id, UpdatePowerSharingGroupDto input);
    Task<PowerSharingGroupDto> GetAsync(Guid id);
    Task<List<PowerSharingGroupListDto>> GetListAsync(GetPowerSharingGroupListDto input);
    Task DeleteAsync(Guid id);
    Task ActivateAsync(Guid id);
    Task DeactivateAsync(Guid id);
    Task<PowerSharingMemberDto> AddMemberAsync(Guid groupId, AddMemberDto input);
    Task RemoveMemberAsync(Guid groupId, Guid connectorId);
    Task<List<PowerAllocationDto>> RecalculateAsync(Guid groupId);
    Task<List<SiteLoadProfileDto>> GetLoadProfilesAsync(Guid groupId, DateTime? from, DateTime? to);
}
