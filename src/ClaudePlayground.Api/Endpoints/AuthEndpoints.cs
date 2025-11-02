using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Application.Validators;
using ClaudePlayground.Domain.Common;
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

            var result = await authService.RegisterAsync(registerDto, ct);

            return result.Match(
                onSuccess: response => Results.Ok(new
                {
                    message = "Registration successful. Please check your email to verify your account.",
                    emailSent = true
                }),
                onFailure: error => error.Type switch
                {
                    ErrorType.Conflict => Results.Conflict(new { error = error.Message }),
                    ErrorType.Validation => Results.BadRequest(new { error = error.Message }),
                    _ => Results.BadRequest(new { error = error.Message })
                });
        })
        .WithName("Register")
        .WithOpenApi()
        .AllowAnonymous();

        group.MapGet("/verify-email", async (string token, IAuthService authService, CancellationToken ct) =>
        {
            var result = await authService.VerifyEmailAsync(token, ct);

            return result.Match(
                onSuccess: () => Results.Ok(new { message = "Email verified successfully. You can now log in." }),
                onFailure: error => Results.BadRequest(new { error = error.Message }));
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

            var result = await authService.LoginAsync(loginDto, ct);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.Type switch
                {
                    ErrorType.Unauthorized => Results.Unauthorized(),
                    ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                    _ => Results.BadRequest(new { error = error.Message })
                });
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

            var result = await authService.GetUserByEmailAsync(email, ct);

            return result.Match(
                onSuccess: user => Results.Ok(user),
                onFailure: error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                    _ => Results.BadRequest(new { error = error.Message })
                });
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

            var result = await authService.ChangePasswordAsync(email, changePasswordDto, ct);

            return result.Match(
                onSuccess: () => Results.Ok(new { message = "Password changed successfully" }),
                onFailure: error => error.Type switch
                {
                    ErrorType.Unauthorized => Results.Unauthorized(),
                    ErrorType.Validation => Results.BadRequest(new { error = error.Message }),
                    _ => Results.BadRequest(new { error = error.Message })
                });
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

            var result = await authService.RequestPasswordResetAsync(forgotPasswordDto, ct);

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

            var result = await authService.ResetPasswordAsync(resetPasswordDto, ct);

            return result.Match(
                onSuccess: () => Results.Ok(new { message = "Password reset successfully. You can now log in with your new password." }),
                onFailure: error => Results.BadRequest(new { error = error.Message }));
        })
        .WithName("ResetPassword")
        .WithOpenApi()
        .AllowAnonymous();

        group.MapPost("/refresh", async (RefreshTokenDto refreshTokenDto, IAuthService authService, CancellationToken ct) =>
        {
            var result = await authService.RefreshTokenAsync(refreshTokenDto.RefreshToken, ct);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.Type switch
                {
                    ErrorType.Unauthorized => Results.Unauthorized(),
                    ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                    _ => Results.BadRequest(new { error = error.Message })
                });
        })
        .WithName("RefreshToken")
        .WithOpenApi()
        .AllowAnonymous();

        return app;
    }
}
