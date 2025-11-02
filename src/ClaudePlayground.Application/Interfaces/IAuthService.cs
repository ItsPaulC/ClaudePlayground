using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Domain.Common;
using ClaudePlayground.Domain.Entities;

namespace ClaudePlayground.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto registerDto, CancellationToken ct = default);
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto loginDto, CancellationToken ct = default);
    Task<Result> VerifyEmailAsync(string token, CancellationToken ct = default);
    Task<Result<UserDto>> GetUserByEmailAsync(string email, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(string email, ChangePasswordDto changePasswordDto, CancellationToken ct = default);
    Task<Result> RequestPasswordResetAsync(ForgotPasswordDto forgotPasswordDto, CancellationToken ct = default);
    Task<Result> ResetPasswordAsync(ResetPasswordDto resetPasswordDto, CancellationToken ct = default);
    Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    // Token generation methods for use by other services
    string GenerateJwtToken(User user);
    Task<string> GenerateAndSaveRefreshTokenAsync(string userId, CancellationToken ct = default);
}
