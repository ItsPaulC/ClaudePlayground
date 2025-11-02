using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Application.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;

namespace ClaudePlayground.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", async (RegisterDto registerDto, IValidator<RegisterDto> validator, IAuthService authService, CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(registerDto, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            AuthResponseDto? response = await authService.RegisterAsync(registerDto, ct);

            // RegisterAsync now returns null on success (email verification required)
            // or null if user already exists
            // We need to determine which case it is by checking the result
            if (response == null)
            {
                // Since RegisterAsync now returns null after successfully sending verification email,
                // we should return a success message instructing the user to check their email
                return Results.Ok(new
                {
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

        group.MapPost("/login", async (LoginDto loginDto, IValidator<LoginDto> validator, IAuthService authService, CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(loginDto, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

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

        group.MapPost("/change-password", async (ChangePasswordDto changePasswordDto, IValidator<ChangePasswordDto> validator, IAuthService authService, HttpContext httpContext, CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(changePasswordDto, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

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

        group.MapPost("/forgot-password", async (ForgotPasswordDto forgotPasswordDto, IValidator<ForgotPasswordDto> validator, IAuthService authService, CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(forgotPasswordDto, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            bool success = await authService.RequestPasswordResetAsync(forgotPasswordDto, ct);

            // Always return success message for security (don't reveal if email exists)
            return Results.Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
        })
        .WithName("ForgotPassword")
        .WithOpenApi()
        .AllowAnonymous();

        group.MapPost("/reset-password", async (ResetPasswordDto resetPasswordDto, IValidator<ResetPasswordDto> validator, IAuthService authService, CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(resetPasswordDto, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            bool success = await authService.ResetPasswordAsync(resetPasswordDto, ct);

            if (!success)
            {
                return Results.BadRequest(new { message = "Password reset failed. Token may be invalid or expired." });
            }

            return Results.Ok(new { message = "Password reset successfully. You can now log in with your new password." });
        })
        .WithName("ResetPassword")
        .WithOpenApi()
        .AllowAnonymous();

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
