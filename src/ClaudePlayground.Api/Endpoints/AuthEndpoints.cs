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

            if (response == null)
            {
                return Results.BadRequest(new { message = "User with this email already exists or registration failed" });
            }

            return Results.Ok(response);
        })
        .WithName("Register")
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

        return app;
    }
}
