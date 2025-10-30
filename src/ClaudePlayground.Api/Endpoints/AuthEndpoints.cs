using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace ClaudePlayground.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", async (RegisterDto registerDto, IAuthService authService, CancellationToken ct) =>
        {
            AuthResponseDto? response = await authService.RegisterAsync(registerDto, ct);

            // RegisterAsync now returns null on success (email verification required)
            // or null if user already exists
            // We need to determine which case it is by checking the result
            if (response == null)
            {
                // Since RegisterAsync now returns null after successfully sending verification email,
                // we should return a success message instructing the user to check their email
                return Results.Ok(new {
                    message = "Registration successful. Please check your email to verify your account.",
                    emailSent = true
                });
            }

            // This shouldn't happen with current implementation, but keeping for safety
            return Results.BadRequest(new { message = "Registration failed" });
        })
        .WithName("Register")
        .WithOpenApi()
        .AllowAnonymous();

        group.MapGet("/verify-email", async (string token, IAuthService authService, CancellationToken ct) =>
        {
            bool success = await authService.VerifyEmailAsync(token, ct);

            if (!success)
            {
                return Results.BadRequest(new { message = "Email verification failed. Token may be invalid or expired." });
            }

            return Results.Ok(new { message = "Email verified successfully. You can now log in." });
        })
        .WithName("VerifyEmail")
        .WithOpenApi()
        .AllowAnonymous();

        group.MapPost("/login", async (LoginDto loginDto, IAuthService authService, CancellationToken ct) =>
        {
            AuthResponseDto? response = await authService.LoginAsync(loginDto, ct);

            if (response == null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(response);
        })
        .WithName("Login")
        .WithOpenApi()
        .AllowAnonymous();

        group.MapGet("/me", async (IAuthService authService, HttpContext httpContext, CancellationToken ct) =>
        {
            string? email = httpContext.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return Results.Unauthorized();
            }

            UserDto? user = await authService.GetUserByEmailAsync(email, ct);

            if (user == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(user);
        })
        .WithName("GetCurrentUser")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/change-password", async (ChangePasswordDto changePasswordDto, IAuthService authService, HttpContext httpContext, CancellationToken ct) =>
        {
            string? email = httpContext.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return Results.Unauthorized();
            }

            bool success = await authService.ChangePasswordAsync(email, changePasswordDto, ct);

            if (!success)
            {
                return Results.BadRequest(new { message = "Failed to change password. Current password may be incorrect." });
            }

            return Results.Ok(new { message = "Password changed successfully" });
        })
        .WithName("ChangePassword")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/refresh", async (RefreshTokenDto refreshTokenDto, IAuthService authService, CancellationToken ct) =>
        {
            AuthResponseDto? response = await authService.RefreshTokenAsync(refreshTokenDto.RefreshToken, ct);

            if (response == null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(response);
        })
        .WithName("RefreshToken")
        .WithOpenApi()
        .AllowAnonymous();

        return app;
    }
}
