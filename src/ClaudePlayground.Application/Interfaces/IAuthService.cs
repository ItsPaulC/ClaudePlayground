using ClaudePlayground.Application.DTOs;

namespace ClaudePlayground.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto, CancellationToken ct = default);
    Task<AuthResponseDto?> LoginAsync(LoginDto loginDto, CancellationToken ct = default);
    Task<bool> VerifyEmailAsync(string token, CancellationToken ct = default);
    Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ChangePasswordAsync(string email, ChangePasswordDto changePasswordDto, CancellationToken ct = default);
    Task<bool> RequestPasswordResetAsync(ForgotPasswordDto forgotPasswordDto, CancellationToken ct = default);
    Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto, CancellationToken ct = default);
    Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}
