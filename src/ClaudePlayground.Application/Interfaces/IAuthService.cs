using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Domain.Common;
using ClaudePlayground.Domain.Entities;

namespace ClaudePlayground.Application.Interfaces;

/// <summary>
/// Service interface for authentication and authorization operations.
/// Handles user registration, login, password management, email verification, and JWT token generation.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user account with email verification.
    /// The user account is created but IsEmailVerified is false until the verification email is confirmed.
    /// A verification email is sent to the user's email address.
    /// </summary>
    /// <param name="registerDto">Registration data including email, password, name, and initial role assignment</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>Result containing authentication response with JWT and refresh tokens, or an error (Conflict if email exists, Validation)</returns>
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto registerDto, CancellationToken ct = default);

    /// <summary>
    /// Authenticates a user with email and password, returning JWT and refresh tokens.
    /// Validates that the user exists, password is correct, email is verified, and account is active.
    /// Updates the LastLoginAt timestamp on successful login.
    /// </summary>
    /// <param name="loginDto">Login credentials (email and password)</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>Result containing authentication response with JWT and refresh tokens, or an error (Unauthorized if credentials invalid or account inactive, NotFound)</returns>
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto loginDto, CancellationToken ct = default);

    /// <summary>
    /// Verifies a user's email address using the verification token sent during registration.
    /// Sets IsEmailVerified to true on successful verification.
    /// </summary>
    /// <param name="token">The email verification token</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>Result indicating success or an error if token is invalid or expired</returns>
    Task<Result> VerifyEmailAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user by their email address.
    /// Used for retrieving the current user's profile from JWT email claim.
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>Result containing the user DTO or an error (NotFound)</returns>
    Task<Result<UserDto>> GetUserByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Changes a user's password after validating the current password.
    /// Requires the user to provide their current password for security.
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="changePasswordDto">Current password and new password</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>Result indicating success or an error (Unauthorized if current password is incorrect, NotFound, Validation)</returns>
    Task<Result> ChangePasswordAsync(string email, ChangePasswordDto changePasswordDto, CancellationToken ct = default);

    /// <summary>
    /// Initiates a password reset request by sending a password reset email.
    /// Generates a reset token and sends it to the user's email.
    /// Returns success regardless of whether the email exists (for security - don't reveal if email is in system).
    /// </summary>
    /// <param name="forgotPasswordDto">Email address to send reset link</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>Result indicating success (always succeeds for security reasons)</returns>
    Task<Result> RequestPasswordResetAsync(ForgotPasswordDto forgotPasswordDto, CancellationToken ct = default);

    /// <summary>
    /// Resets a user's password using the reset token sent via email.
    /// Validates the reset token and sets the new password.
    /// </summary>
    /// <param name="resetPasswordDto">Reset token and new password</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>Result indicating success or an error if token is invalid or expired</returns>
    Task<Result> ResetPasswordAsync(ResetPasswordDto resetPasswordDto, CancellationToken ct = default);

    /// <summary>
    /// Refreshes an expired JWT access token using a valid refresh token.
    /// Generates new JWT and refresh tokens, and revokes the old refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>Result containing new authentication response with JWT and refresh tokens, or an error (Unauthorized if refresh token is invalid or expired, NotFound)</returns>
    Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Generates a JWT access token for a user containing claims for user ID, email, tenant, and roles.
    /// This method is used by other services (e.g., BusinessService) when creating users programmatically.
    /// </summary>
    /// <param name="user">The user entity to generate a token for</param>
    /// <returns>The JWT access token as a string</returns>
    string GenerateJwtToken(User user);

    /// <summary>
    /// Generates a new refresh token, saves it to the database, and returns it.
    /// This method is used by other services (e.g., BusinessService) when creating users programmatically.
    /// Refresh tokens are valid for 30 days by default.
    /// </summary>
    /// <param name="userId">The user ID to associate with the refresh token</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>The refresh token as a string</returns>
    Task<string> GenerateAndSaveRefreshTokenAsync(string userId, CancellationToken ct = default);
}
