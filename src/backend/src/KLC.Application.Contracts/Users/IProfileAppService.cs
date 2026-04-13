using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace KLC.Users;

/// <summary>
/// Application service for driver profile business logic that involves mutations:
/// phone change (OTP + verification), account deletion.
/// Read-only and file-upload operations stay in the BFF layer.
/// </summary>
public interface IProfileAppService : IApplicationService
{
    /// <summary>
    /// Validate the new phone number, generate an OTP, store it in Redis, and log it.
    /// </summary>
    Task RequestPhoneChangeAsync(Guid userId, string newPhoneNumber);

    /// <summary>
    /// Verify the OTP for phone change, update the user's phone number, and clean up Redis.
    /// </summary>
    Task ConfirmPhoneChangeAsync(Guid userId, string newPhoneNumber, string otp);

    /// <summary>
    /// Validate no active sessions exist, then soft-delete the user account.
    /// </summary>
    Task DeleteAccountAsync(Guid userId);
}
