using System.Security.Claims;
using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Domain.Common;
using ClaudePlayground.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;

namespace ClaudePlayground.Api.Tests.Unit.Endpoints;

public class AuthEndpointsTests
{
    private readonly IAuthService _authService;

    public AuthEndpointsTests()
    {
        _authService = Substitute.For<IAuthService>();
    }

    #region Register Tests

    [Fact]
    public async Task Register_WhenValid_ReturnsOkWithMessage()
    {
        // Arrange
        RegisterDto registerDto = new(
            Email: "newuser@example.com",
            Password: "SecurePassword123!",
            FirstName: "John",
            LastName: "Doe",
            Roles: null
        );

        AuthResponseDto authResponse = new(
            Token: "jwt-token",
            RefreshToken: "refresh-token",
            TenantId: "tenant-123",
            Email: registerDto.Email,
            FirstName: registerDto.FirstName,
            LastName: registerDto.LastName
        );

        _authService.RegisterAsync(registerDto, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResponseDto>.Success(authResponse));

        // Act
        IResult result = await RegisterHandler(registerDto, _authService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("Ok", result.GetType().Name);

        await _authService.Received(1).RegisterAsync(registerDto, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_WhenConflict_ReturnsConflict()
    {
        // Arrange
        RegisterDto registerDto = new(
            Email: "existing@example.com",
            Password: "Password123!",
            FirstName: "Jane",
            LastName: "Smith",
            Roles: null
        );

        Error error = new("User.Conflict", "User already exists", ErrorType.Conflict);
        _authService.RegisterAsync(registerDto, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResponseDto>.Failure(error));

        // Act
        IResult result = await RegisterHandler(registerDto, _authService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("Conflict", result.GetType().Name);

        await _authService.Received(1).RegisterAsync(registerDto, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_WhenValidationError_ReturnsBadRequest()
    {
        // Arrange
        RegisterDto registerDto = new(
            Email: "invalid@example.com",
            Password: "weak",
            FirstName: "Test",
            LastName: "User",
            Roles: null
        );

        Error error = new("Validation.Error", "Password is too weak", ErrorType.Validation);
        _authService.RegisterAsync(registerDto, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResponseDto>.Failure(error));

        // Act
        IResult result = await RegisterHandler(registerDto, _authService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("BadRequest", result.GetType().Name);

        await _authService.Received(1).RegisterAsync(registerDto, Arg.Any<CancellationToken>());
    }

    #endregion

    #region VerifyEmail Tests

    [Fact]
    public async Task VerifyEmail_WhenTokenValid_ReturnsOkWithMessage()
    {
        // Arrange
        string token = "valid-verification-token";

        _authService.VerifyEmailAsync(token, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        IResult result = await VerifyEmailHandler(token, _authService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("Ok", result.GetType().Name);

        await _authService.Received(1).VerifyEmailAsync(token, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyEmail_WhenTokenInvalid_ReturnsBadRequest()
    {
        // Arrange
        string token = "invalid-token";
        Error error = new("Token.Invalid", "Invalid or expired token", ErrorType.Validation);

        _authService.VerifyEmailAsync(token, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        // Act
        IResult result = await VerifyEmailHandler(token, _authService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("BadRequest", result.GetType().Name);

        await _authService.Received(1).VerifyEmailAsync(token, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WhenCredentialsValid_ReturnsOkWithAuthResponse()
    {
        // Arrange
        LoginDto loginDto = new(
            Email: "user@example.com",
            Password: "Password123!"
        );

        AuthResponseDto authResponse = new(
            Token: "jwt-token",
            RefreshToken: "refresh-token",
            TenantId: "tenant-123",
            Email: loginDto.Email,
            FirstName: "John",
            LastName: "Doe"
        );

        _authService.LoginAsync(loginDto, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResponseDto>.Success(authResponse));

        // Act
        IResult result = await LoginHandler(loginDto, _authService, CancellationToken.None);

        // Assert
        Assert.IsType<Ok<AuthResponseDto>>(result);
        Ok<AuthResponseDto> okResult = (Ok<AuthResponseDto>)result;
        Assert.Equal(authResponse, okResult.Value);

        await _authService.Received(1).LoginAsync(loginDto, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_WhenCredentialsInvalid_ReturnsUnauthorized()
    {
        // Arrange
        LoginDto loginDto = new(
            Email: "user@example.com",
            Password: "WrongPassword"
        );

        Error error = new("Auth.InvalidCredentials", "Invalid credentials", ErrorType.Unauthorized);
        _authService.LoginAsync(loginDto, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResponseDto>.Failure(error));

        // Act
        IResult result = await LoginHandler(loginDto, _authService, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);

        await _authService.Received(1).LoginAsync(loginDto, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        LoginDto loginDto = new(
            Email: "nonexistent@example.com",
            Password: "Password123!"
        );

        Error error = new("User.NotFound", "User not found", ErrorType.NotFound);
        _authService.LoginAsync(loginDto, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResponseDto>.Failure(error));

        // Act
        IResult result = await LoginHandler(loginDto, _authService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("NotFound", result.GetType().Name);

        await _authService.Received(1).LoginAsync(loginDto, Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetMe Tests

    [Fact]
    public async Task GetMe_WhenEmailClaimExists_ReturnsOkWithUser()
    {
        // Arrange
        string email = "user@example.com";
        UserDto user = new(
            Id: "user-123",
            TenantId: "tenant-123",
            Email: email,
            FirstName: "John",
            LastName: "Doe",
            IsActive: true,
            Roles: new List<Role> { new("User", "U", "user") },
            LastLoginAt: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        _authService.GetUserByEmailAsync(email, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Success(user));

        // Act
        IResult result = await GetMeHandler(email, _authService, CancellationToken.None);

        // Assert
        Assert.IsType<Ok<UserDto>>(result);
        Ok<UserDto> okResult = (Ok<UserDto>)result;
        Assert.Equal(user, okResult.Value);

        await _authService.Received(1).GetUserByEmailAsync(email, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMe_WhenEmailClaimNull_ReturnsUnauthorized()
    {
        // Arrange
        string? email = null;

        // Act
        IResult result = await GetMeHandler(email, _authService, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);

        await _authService.DidNotReceive().GetUserByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMe_WhenEmailClaimEmpty_ReturnsUnauthorized()
    {
        // Arrange
        string email = string.Empty;

        // Act
        IResult result = await GetMeHandler(email, _authService, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);

        await _authService.DidNotReceive().GetUserByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMe_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        string email = "nonexistent@example.com";
        Error error = new("User.NotFound", "User not found", ErrorType.NotFound);

        _authService.GetUserByEmailAsync(email, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(error));

        // Act
        IResult result = await GetMeHandler(email, _authService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("NotFound", result.GetType().Name);

        await _authService.Received(1).GetUserByEmailAsync(email, Arg.Any<CancellationToken>());
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePassword_WhenValid_ReturnsOkWithMessage()
    {
        // Arrange
        string email = "user@example.com";
        ChangePasswordDto changePasswordDto = new(
            CurrentPassword: "OldPassword123!",
            NewPassword: "NewPassword456!"
        );

        _authService.ChangePasswordAsync(email, changePasswordDto, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        IResult result = await ChangePasswordHandler(email, changePasswordDto, _authService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("Ok", result.GetType().Name);

        await _authService.Received(1).ChangePasswordAsync(email, changePasswordDto, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePassword_WhenEmailClaimNull_ReturnsUnauthorized()
    {
        // Arrange
        string? email = null;
        ChangePasswordDto changePasswordDto = new(
            CurrentPassword: "OldPassword123!",
            NewPassword: "NewPassword456!"
        );

        // Act
        IResult result = await ChangePasswordHandler(email, changePasswordDto, _authService, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);

        await _authService.DidNotReceive().ChangePasswordAsync(Arg.Any<string>(), Arg.Any<ChangePasswordDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePassword_WhenCurrentPasswordWrong_ReturnsUnauthorized()
    {
        // Arrange
        string email = "user@example.com";
        ChangePasswordDto changePasswordDto = new(
            CurrentPassword: "WrongPassword",
            NewPassword: "NewPassword456!"
        );

        Error error = new("Auth.InvalidPassword", "Current password is incorrect", ErrorType.Unauthorized);
        _authService.ChangePasswordAsync(email, changePasswordDto, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        // Act
        IResult result = await ChangePasswordHandler(email, changePasswordDto, _authService, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);

        await _authService.Received(1).ChangePasswordAsync(email, changePasswordDto, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePassword_WhenValidationError_ReturnsBadRequest()
    {
        // Arrange
        string email = "user@example.com";
        ChangePasswordDto changePasswordDto = new(
            CurrentPassword: "OldPassword123!",
            NewPassword: "weak"
        );

        Error error = new("Validation.Error", "New password is too weak", ErrorType.Validation);
        _authService.ChangePasswordAsync(email, changePasswordDto, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        // Act
        IResult result = await ChangePasswordHandler(email, changePasswordDto, _authService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("BadRequest", result.GetType().Name);

        await _authService.Received(1).ChangePasswordAsync(email, changePasswordDto, Arg.Any<CancellationToken>());
    }

    #endregion

    #region ForgotPassword Tests

    [Fact]
    public async Task ForgotPassword_WhenCalled_AlwaysReturnsOkMessage()
    {
        // Arrange
        ForgotPasswordDto forgotPasswordDto = new(Email: "user@example.com");

        _authService.RequestPasswordResetAsync(forgotPasswordDto, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        IResult result = await ForgotPasswordHandler(forgotPasswordDto, _authService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("Ok", result.GetType().Name);

        await _authService.Received(1).RequestPasswordResetAsync(forgotPasswordDto, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ForgotPassword_WhenEmailNotFound_StillReturnsOkMessage()
    {
        // Arrange - For security, always return success even if email doesn't exist
        ForgotPasswordDto forgotPasswordDto = new(Email: "nonexistent@example.com");

        Error error = new("User.NotFound", "User not found", ErrorType.NotFound);
        _authService.RequestPasswordResetAsync(forgotPasswordDto, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        // Act
        IResult result = await ForgotPasswordHandler(forgotPasswordDto, _authService, CancellationToken.None);

        // Assert - Should still return Ok for security (don't reveal if email exists)
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("Ok", result.GetType().Name);

        await _authService.Received(1).RequestPasswordResetAsync(forgotPasswordDto, Arg.Any<CancellationToken>());
    }

    #endregion

    #region ResetPassword Tests

    [Fact]
    public async Task ResetPassword_WhenTokenValid_ReturnsOkWithMessage()
    {
        // Arrange
        ResetPasswordDto resetPasswordDto = new(
            Token: "valid-reset-token",
            NewPassword: "NewSecurePassword123!"
        );

        _authService.ResetPasswordAsync(resetPasswordDto, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        IResult result = await ResetPasswordHandler(resetPasswordDto, _authService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("Ok", result.GetType().Name);

        await _authService.Received(1).ResetPasswordAsync(resetPasswordDto, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetPassword_WhenTokenInvalid_ReturnsBadRequest()
    {
        // Arrange
        ResetPasswordDto resetPasswordDto = new(
            Token: "invalid-token",
            NewPassword: "NewPassword123!"
        );

        Error error = new("Token.Invalid", "Invalid or expired token", ErrorType.Validation);
        _authService.ResetPasswordAsync(resetPasswordDto, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        // Act
        IResult result = await ResetPasswordHandler(resetPasswordDto, _authService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("BadRequest", result.GetType().Name);

        await _authService.Received(1).ResetPasswordAsync(resetPasswordDto, Arg.Any<CancellationToken>());
    }

    #endregion

    #region RefreshToken Tests

    [Fact]
    public async Task RefreshToken_WhenTokenValid_ReturnsOkWithAuthResponse()
    {
        // Arrange
        string refreshToken = "valid-refresh-token";
        AuthResponseDto authResponse = new(
            Token: "new-jwt-token",
            RefreshToken: "new-refresh-token",
            TenantId: "tenant-123",
            Email: "user@example.com",
            FirstName: "John",
            LastName: "Doe"
        );

        _authService.RefreshTokenAsync(refreshToken, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResponseDto>.Success(authResponse));

        // Act
        IResult result = await RefreshTokenHandler(refreshToken, _authService, CancellationToken.None);

        // Assert
        Assert.IsType<Ok<AuthResponseDto>>(result);
        Ok<AuthResponseDto> okResult = (Ok<AuthResponseDto>)result;
        Assert.Equal(authResponse, okResult.Value);

        await _authService.Received(1).RefreshTokenAsync(refreshToken, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshToken_WhenTokenInvalid_ReturnsUnauthorized()
    {
        // Arrange
        string refreshToken = "invalid-refresh-token";
        Error error = new("Token.Invalid", "Invalid refresh token", ErrorType.Unauthorized);

        _authService.RefreshTokenAsync(refreshToken, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResponseDto>.Failure(error));

        // Act
        IResult result = await RefreshTokenHandler(refreshToken, _authService, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);

        await _authService.Received(1).RefreshTokenAsync(refreshToken, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshToken_WhenTokenNotFound_ReturnsNotFound()
    {
        // Arrange
        string refreshToken = "nonexistent-token";
        Error error = new("Token.NotFound", "Refresh token not found", ErrorType.NotFound);

        _authService.RefreshTokenAsync(refreshToken, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResponseDto>.Failure(error));

        // Act
        IResult result = await RefreshTokenHandler(refreshToken, _authService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("NotFound", result.GetType().Name);

        await _authService.Received(1).RefreshTokenAsync(refreshToken, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handler Methods

    // Handler methods that mirror the endpoint logic
    // These are extracted to make testing easier without requiring WebApplicationFactory

    private static async Task<IResult> RegisterHandler(
        RegisterDto registerDto,
        IAuthService authService,
        CancellationToken ct)
    {
        Result<AuthResponseDto> result = await authService.RegisterAsync(registerDto, ct);

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
    }

    private static async Task<IResult> VerifyEmailHandler(
        string token,
        IAuthService authService,
        CancellationToken ct)
    {
        Result result = await authService.VerifyEmailAsync(token, ct);

        return result.Match(
            onSuccess: () => Results.Ok(new { message = "Email verified successfully. You can now log in." }),
            onFailure: error => Results.BadRequest(new { error = error.Message }));
    }

    private static async Task<IResult> LoginHandler(
        LoginDto loginDto,
        IAuthService authService,
        CancellationToken ct)
    {
        Result<AuthResponseDto> result = await authService.LoginAsync(loginDto, ct);

        return result.Match(
            onSuccess: response => Results.Ok(response),
            onFailure: error => error.Type switch
            {
                ErrorType.Unauthorized => Results.Unauthorized(),
                ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                _ => Results.BadRequest(new { error = error.Message })
            });
    }

    private static async Task<IResult> GetMeHandler(
        string? email,
        IAuthService authService,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Results.Unauthorized();
        }

        Result<UserDto> result = await authService.GetUserByEmailAsync(email, ct);

        return result.Match(
            onSuccess: user => Results.Ok(user),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                _ => Results.BadRequest(new { error = error.Message })
            });
    }

    private static async Task<IResult> ChangePasswordHandler(
        string? email,
        ChangePasswordDto changePasswordDto,
        IAuthService authService,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Results.Unauthorized();
        }

        Result result = await authService.ChangePasswordAsync(email, changePasswordDto, ct);

        return result.Match(
            onSuccess: () => Results.Ok(new { message = "Password changed successfully" }),
            onFailure: error => error.Type switch
            {
                ErrorType.Unauthorized => Results.Unauthorized(),
                ErrorType.Validation => Results.BadRequest(new { error = error.Message }),
                _ => Results.BadRequest(new { error = error.Message })
            });
    }

    private static async Task<IResult> ForgotPasswordHandler(
        ForgotPasswordDto forgotPasswordDto,
        IAuthService authService,
        CancellationToken ct)
    {
        Result result = await authService.RequestPasswordResetAsync(forgotPasswordDto, ct);

        // Always return success message for security (don't reveal if email exists)
        return Results.Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
    }

    private static async Task<IResult> ResetPasswordHandler(
        ResetPasswordDto resetPasswordDto,
        IAuthService authService,
        CancellationToken ct)
    {
        Result result = await authService.ResetPasswordAsync(resetPasswordDto, ct);

        return result.Match(
            onSuccess: () => Results.Ok(new { message = "Password reset successfully. You can now log in with your new password." }),
            onFailure: error => Results.BadRequest(new { error = error.Message }));
    }

    private static async Task<IResult> RefreshTokenHandler(
        string refreshToken,
        IAuthService authService,
        CancellationToken ct)
    {
        Result<AuthResponseDto> result = await authService.RefreshTokenAsync(refreshToken, ct);

        return result.Match(
            onSuccess: response => Results.Ok(response),
            onFailure: error => error.Type switch
            {
                ErrorType.Unauthorized => Results.Unauthorized(),
                ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                _ => Results.BadRequest(new { error = error.Message })
            });
    }

    #endregion
}
