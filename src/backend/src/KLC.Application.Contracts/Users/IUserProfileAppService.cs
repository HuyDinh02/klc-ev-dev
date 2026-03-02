using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace KLC.Users;

public interface IUserProfileAppService : IApplicationService
{
    Task<UserProfileDto> GetProfileAsync();

    Task<UserProfileDto> UpdateProfileAsync(UpdateProfileDto input);

    Task UpdatePhoneAsync(UpdatePhoneDto input);

    Task VerifyPhoneAsync(VerifyPhoneDto input);

    Task UpdateEmailAsync(UpdateEmailDto input);

    Task VerifyEmailAsync(VerifyEmailDto input);

    Task ChangePasswordAsync(ChangePasswordDto input);

    Task<UserStatisticsDto> GetStatisticsAsync();

    Task DeactivateAccountAsync();
}
