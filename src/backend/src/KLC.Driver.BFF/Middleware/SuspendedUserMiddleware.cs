using System.Security.Claims;
using KLC.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KLC.Driver.Middleware;

/// <summary>
/// Blocks suspended users from accessing any authenticated endpoint.
/// Checks AppUser.IsActive on every request with a valid JWT.
/// Without this, a user suspended while holding a valid token (1h expiry)
/// could continue using the system until the token expires.
/// </summary>
public class SuspendedUserMiddleware
{
    private readonly RequestDelegate _next;

    public SuspendedUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
        {
            var dbContext = context.RequestServices.GetRequiredService<KLCDbContext>();

            var isActive = await dbContext.AppUsers
                .AsNoTracking()
                .Where(u => u.IdentityUserId == userGuid)
                .Select(u => u.IsActive)
                .FirstOrDefaultAsync();

            if (!isActive)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"error\":{\"code\":\"ACCOUNT_SUSPENDED\",\"message\":\"Your account has been suspended.\"}}");
                return;
            }
        }

        await _next(context);
    }
}
