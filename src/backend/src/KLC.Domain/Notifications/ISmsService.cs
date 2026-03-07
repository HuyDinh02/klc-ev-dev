using System.Threading.Tasks;

namespace KLC.Notifications;

/// <summary>
/// Interface for sending SMS messages (OTP, notifications).
/// </summary>
public interface ISmsService
{
    Task SendAsync(string phoneNumber, string message);
}
