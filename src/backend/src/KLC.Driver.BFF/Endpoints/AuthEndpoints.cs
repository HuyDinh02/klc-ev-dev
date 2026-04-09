using System.Security.Claims;
using KLC.Driver.Services;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Driver.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth")
            .RequireRateLimiting("auth");

        // POST /api/v1/auth/register
        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            IAuthBffService authService) =>
        {
            var result = await authService.RegisterAsync(request);
            return result.Success
                ? Results.Created($"/api/v1/profile", result)
                : Results.BadRequest(new { error = new { code = "REGISTRATION_FAILED", message = result.Error } });
        })
        .WithName("Register")
        .WithSummary("Register a new user with phone number")
        .Produces<RegisterResultDto>(201)
        .Produces(400);

        // POST /api/v1/auth/verify-phone
        group.MapPost("/verify-phone", async (
            [FromBody] VerifyPhoneRequest request,
            IAuthBffService authService) =>
        {
            var result = await authService.VerifyPhoneAsync(request);
            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(new { error = new { code = "VERIFY_FAILED", message = result.Error } });
        })
        .WithName("VerifyPhone")
        .WithSummary("Verify phone number with OTP")
        .Produces<VerifyOtpResultDto>(200)
        .Produces(400);

        // POST /api/v1/auth/resend-otp
        group.MapPost("/resend-otp", async (
            [FromBody] ResendOtpRequest request,
            IAuthBffService authService) =>
        {
            await authService.ResendOtpAsync(request);
            return Results.Ok(new { message = "OTP sent successfully" });
        })
        .WithName("ResendOtp")
        .WithSummary("Resend OTP to phone number")
        .Produces<object>(200);

        // POST /api/v1/auth/login
        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            IAuthBffService authService) =>
        {
            var result = await authService.LoginAsync(request);
            return result.Success
                ? Results.Ok(result)
                : Results.Unauthorized();
        })
        .WithName("Login")
        .WithSummary("Login with phone number and password")
        .Produces<LoginResultDto>(200)
        .Produces(401);

        // POST /api/v1/auth/refresh-token
        group.MapPost("/refresh-token", async (
            [FromBody] RefreshTokenRequest request,
            IAuthBffService authService) =>
        {
            var result = await authService.RefreshTokenAsync(request);
            return result.Success
                ? Results.Ok(result)
                : Results.Unauthorized();
        })
        .WithName("RefreshToken")
        .WithSummary("Refresh access token")
        .Produces<LoginResultDto>(200)
        .Produces(401);

        // POST /api/v1/auth/logout
        group.MapPost("/logout", async (
            [FromBody] RefreshTokenRequest? request,
            ClaimsPrincipal user,
            IAuthBffService authService) =>
        {
            var userId = GetUserId(user);
            await authService.LogoutAsync(userId, request?.RefreshToken);
            return Results.NoContent();
        })
        .WithName("Logout")
        .WithSummary("Logout and invalidate refresh token")
        .RequireAuthorization()
        .Produces(204);

        // POST /api/v1/auth/forgot-password
        group.MapPost("/forgot-password", async (
            [FromBody] ForgotPasswordRequest request,
            IAuthBffService authService) =>
        {
            await authService.ForgotPasswordAsync(request);
            return Results.Ok(new { message = "If the phone number is registered, an OTP has been sent" });
        })
        .WithName("ForgotPassword")
        .WithSummary("Request password reset OTP")
        .Produces<object>(200);

        // POST /api/v1/auth/reset-password
        group.MapPost("/reset-password", async (
            [FromBody] ResetPasswordRequest request,
            IAuthBffService authService) =>
        {
            await authService.ResetPasswordAsync(request);
            return Results.Ok(new { message = "Password reset successful" });
        })
        .WithName("ResetPassword")
        .WithSummary("Reset password with OTP")
        .Produces<object>(200)
        .Produces(400);

        // POST /api/v1/auth/firebase-reset-password — Reset password using Firebase Phone Auth token
        group.MapPost("/firebase-reset-password", async (
            [FromBody] FirebaseResetPasswordRequest request,
            IAuthBffService authService) =>
        {
            await authService.ResetPasswordWithFirebaseAsync(request);
            return Results.Ok(new { message = "Password reset successful" });
        })
        .WithName("FirebaseResetPassword")
        .WithSummary("Reset password using Firebase Phone Auth ID token")
        .Produces<object>(200)
        .Produces(400);

        // POST /api/v1/auth/change-password
        group.MapPost("/change-password", async (
            [FromBody] ChangePasswordRequest request,
            ClaimsPrincipal user,
            IAuthBffService authService) =>
        {
            var userId = GetUserId(user);
            await authService.ChangePasswordAsync(userId, request);
            return Results.Ok(new { message = "Password changed successfully" });
        })
        .WithName("ChangePassword")
        .WithSummary("Change password (authenticated)")
        .RequireAuthorization()
        .Produces<object>(200)
        .Produces(400);

        // POST /api/v1/auth/firebase-phone — Login/register with Firebase Phone Auth
        group.MapPost("/firebase-phone", async (
            [FromBody] FirebasePhoneLoginRequest request,
            IAuthBffService authService) =>
        {
            var result = await authService.FirebasePhoneLoginAsync(request);
            return result.Success
                ? Results.Ok(result)
                : Results.Unauthorized();
        })
        .WithName("FirebasePhoneLogin")
        .WithSummary("Login or register with Firebase Phone Auth ID token")
        .Produces<LoginResultDto>(200)
        .Produces(401);

        // POST /api/v1/auth/social-login
        group.MapPost("/social-login", async (
            [FromBody] SocialLoginRequest request,
            IAuthBffService authService) =>
        {
            var result = await authService.SocialLoginAsync(request);
            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(new { error = new { code = "SOCIAL_LOGIN_FAILED", message = result.Error } });
        })
        .WithName("SocialLogin")
        .WithSummary("Login with social provider (Google, Apple, Facebook)")
        .Produces<LoginResultDto>(200)
        .Produces(400);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
