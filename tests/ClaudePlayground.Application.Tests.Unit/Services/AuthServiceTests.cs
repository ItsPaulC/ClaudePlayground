using ClaudePlayground.Application.Configuration;
using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Application.Services;
using ClaudePlayground.Domain.Common;
using ClaudePlayground.Domain.Entities;
using ClaudePlayground.Domain.ValueObjects;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace ClaudePlayground.Application.Tests.Unit.Services;

public class AuthServiceTests
{
    // System Under Test
    private readonly AuthService _sut;

    // Mocks (member variables)
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly IEmailService _emailService;

    public AuthServiceTests()
    {
        // Initialize mocks
        _userRepository = Substitute.For<IRepository<User>>();
        _refreshTokenRepository = Substitute.For<IRepository<RefreshToken>>();
        _emailService = Substitute.For<IEmailService>();

        // Create test JWT settings
        JwtSettings jwtSettings = new()
        {
            SecretKey = "ThisIsASecretKeyForTestingPurposesOnly1234567890",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        // Create System Under Test
        _sut = new AuthService(
            _userRepository,
            _refreshTokenRepository,
            jwtSettings,
            _emailService
        );
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithNewUser_ShouldCreateUserAndSendVerificationEmail()
    {
        // Arrange
        var registerDto = new RegisterDto(
            "test@example.com",
            "password123",
            "Test",
            "User",
            null
        );

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User>());

        var createdUser = new User
        {
            Id = "user123",
            Email = "test@example.com",
            IsEmailVerified = false
        };

        _userRepository.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(createdUser);

        // Act
        var result = await _sut.RegisterAsync(registerDto, CancellationToken.None);

        // Assert
        Assert.Null(result); // Should return null after sending verification email
        await _emailService.Received(1).SendEmailVerificationAsync(
            Arg.Is<string>(e => e == "test@example.com"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );
        await _userRepository.Received(1).CreateAsync(
            Arg.Is<User>(u => u.Email == "test@example.com" && !u.IsEmailVerified),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUser_ShouldReturnNull()
    {
        // Arrange
        var registerDto = new RegisterDto(
            "existing@example.com",
            "password123",
            "Test",
            "User",
            null
        );

        var existingUser = new User
        {
            Id = "existing123",
            Email = "existing@example.com",
            TenantId = "tenant123"
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { existingUser });

        // Act
        var result = await _sut.RegisterAsync(registerDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        await _userRepository.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _emailService.DidNotReceive().SendEmailVerificationAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );
    }

    #endregion

    #region VerifyEmailAsync Tests

    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_ShouldVerifyEmailAndReturnTrue()
    {
        // Arrange
        var token = "valid-token";
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            IsEmailVerified = false,
            EmailVerificationToken = token,
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(1),
            TenantId = "tenant123"
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });

        // Act
        var result = await _sut.VerifyEmailAsync(token, CancellationToken.None);

        // Assert
        Assert.True(result);
        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.IsEmailVerified && u.EmailVerificationToken == null),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task VerifyEmailAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var token = "invalid-token";

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User>());

        // Act
        var result = await _sut.VerifyEmailAsync(token, CancellationToken.None);

        // Assert
        Assert.False(result);
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyEmailAsync_WithExpiredToken_ShouldReturnFalse()
    {
        // Arrange
        var token = "expired-token";
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            IsEmailVerified = false,
            EmailVerificationToken = token,
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            TenantId = "tenant123"
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });

        // Act
        var result = await _sut.VerifyEmailAsync(token, CancellationToken.None);

        // Assert
        Assert.False(result);
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyEmailAsync_WithAlreadyVerifiedEmail_ShouldReturnTrue()
    {
        // Arrange
        var token = "valid-token";
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            IsEmailVerified = true, // Already verified
            EmailVerificationToken = token,
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(1),
            TenantId = "tenant123"
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });

        // Act
        var result = await _sut.VerifyEmailAsync(token, CancellationToken.None);

        // Assert
        Assert.True(result);
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        var loginDto = new LoginDto("test@example.com", "password123");
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            IsEmailVerified = true,
            IsActive = true,
            FirstName = "Test",
            LastName = "User",
            TenantId = "tenant123",
            Roles = new List<Role> { Roles.User }
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });

        var refreshToken = new RefreshToken
        {
            Token = "refresh-token",
            UserId = "user123",
            TenantId = "tenant123"
        };

        _refreshTokenRepository.CreateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>())
            .Returns(refreshToken);

        // Act
        var result = await _sut.LoginAsync(loginDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test", result.FirstName);
        Assert.Equal("User", result.LastName);
        Assert.Equal("tenant123", result.TenantId);
        Assert.NotEmpty(result.Token);
        Assert.NotEmpty(result.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldReturnNull()
    {
        // Arrange
        var loginDto = new LoginDto("nonexistent@example.com", "password123");

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User>());

        // Act
        var result = await _sut.LoginAsync(loginDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldReturnNull()
    {
        // Arrange
        var loginDto = new LoginDto("test@example.com", "wrongpassword");
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword"),
            IsEmailVerified = true,
            IsActive = true,
            TenantId = "tenant123"
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });

        // Act
        var result = await _sut.LoginAsync(loginDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithUnverifiedEmail_ShouldReturnNull()
    {
        // Arrange
        var loginDto = new LoginDto("test@example.com", "password123");
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            IsEmailVerified = false, // Not verified
            IsActive = true,
            TenantId = "tenant123"
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });

        // Act
        var result = await _sut.LoginAsync(loginDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ShouldReturnNull()
    {
        // Arrange
        var loginDto = new LoginDto("test@example.com", "password123");
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            IsEmailVerified = true,
            IsActive = false, // Inactive
            TenantId = "tenant123"
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });

        // Act
        var result = await _sut.LoginAsync(loginDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetUserByEmailAsync Tests

    [Fact]
    public async Task GetUserByEmailAsync_WithExistingEmail_ShouldReturnUserDto()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User
        {
            Id = "user123",
            Email = email,
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            TenantId = "tenant123",
            Roles = new List<Role> { Roles.User },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });

        // Act
        var result = await _sut.GetUserByEmailAsync(email, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user123", result.Id);
        Assert.Equal(email, result.Email);
        Assert.Equal("Test", result.FirstName);
        Assert.Equal("User", result.LastName);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithNonExistingEmail_ShouldReturnNull()
    {
        // Arrange
        var email = "nonexistent@example.com";

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User>());

        // Act
        var result = await _sut.GetUserByEmailAsync(email, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ChangePasswordAsync Tests

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_ShouldChangePasswordAndReturnTrue()
    {
        // Arrange
        var email = "test@example.com";
        var changePasswordDto = new ChangePasswordDto("oldpassword", "newpassword123");
        var user = new User
        {
            Id = "user123",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpassword"),
            TenantId = "tenant123"
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });

        // Act
        var result = await _sut.ChangePasswordAsync(email, changePasswordDto, CancellationToken.None);

        // Assert
        Assert.True(result);

        // Verify UpdateAsync was called
        await _userRepository.Received(1).UpdateAsync(
            Arg.Any<User>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInvalidCurrentPassword_ShouldReturnFalse()
    {
        // Arrange
        var email = "test@example.com";
        var changePasswordDto = new ChangePasswordDto("wrongpassword", "newpassword123");
        var user = new User
        {
            Id = "user123",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword"),
            TenantId = "tenant123"
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });

        // Act
        var result = await _sut.ChangePasswordAsync(email, changePasswordDto, CancellationToken.None);

        // Assert
        Assert.False(result);
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePasswordAsync_WithNonExistingUser_ShouldReturnFalse()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var changePasswordDto = new ChangePasswordDto("password", "newpassword");

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User>());

        // Act
        var result = await _sut.ChangePasswordAsync(email, changePasswordDto, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region RequestPasswordResetAsync Tests

    [Fact]
    public async Task RequestPasswordResetAsync_WithValidVerifiedUser_ShouldSendResetEmail()
    {
        // Arrange
        var forgotPasswordDto = new ForgotPasswordDto("test@example.com");
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            IsEmailVerified = true,
            TenantId = "tenant123"
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });

        // Act
        var result = await _sut.RequestPasswordResetAsync(forgotPasswordDto, CancellationToken.None);

        // Assert
        Assert.True(result);
        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.PasswordResetToken != null && u.PasswordResetTokenExpiresAt != null),
            Arg.Any<CancellationToken>()
        );
        await _emailService.Received(1).SendPasswordResetAsync(
            Arg.Is<string>(e => e == "test@example.com"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithNonExistingUser_ShouldReturnTrueWithoutSendingEmail()
    {
        // Arrange
        var forgotPasswordDto = new ForgotPasswordDto("nonexistent@example.com");

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User>());

        // Act
        var result = await _sut.RequestPasswordResetAsync(forgotPasswordDto, CancellationToken.None);

        // Assert
        Assert.True(result); // Always returns true for security
        await _emailService.DidNotReceive().SendPasswordResetAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithUnverifiedUser_ShouldReturnTrueWithoutSendingEmail()
    {
        // Arrange
        var forgotPasswordDto = new ForgotPasswordDto("test@example.com");
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            IsEmailVerified = false, // Not verified
            TenantId = "tenant123"
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });

        // Act
        var result = await _sut.RequestPasswordResetAsync(forgotPasswordDto, CancellationToken.None);

        // Assert
        Assert.True(result); // Always returns true for security
        await _emailService.DidNotReceive().SendPasswordResetAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldResetPasswordAndReturnTrue()
    {
        // Arrange
        var resetPasswordDto = new ResetPasswordDto("valid-reset-token", "newpassword123");
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpassword"),
            PasswordResetToken = "valid-reset-token",
            PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1),
            TenantId = "tenant123"
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });

        // Act
        var result = await _sut.ResetPasswordAsync(resetPasswordDto, CancellationToken.None);

        // Assert
        Assert.True(result);

        // Verify UpdateAsync was called
        await _userRepository.Received(1).UpdateAsync(
            Arg.Any<User>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var resetPasswordDto = new ResetPasswordDto("invalid-token", "newpassword123");

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User>());

        // Act
        var result = await _sut.ResetPasswordAsync(resetPasswordDto, CancellationToken.None);

        // Assert
        Assert.False(result);
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_ShouldReturnFalse()
    {
        // Arrange
        var resetPasswordDto = new ResetPasswordDto("expired-token", "newpassword123");
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            PasswordResetToken = "expired-token",
            PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            TenantId = "tenant123"
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });

        // Act
        var result = await _sut.ResetPasswordAsync(resetPasswordDto, CancellationToken.None);

        // Assert
        Assert.False(result);
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewAuthResponse()
    {
        // Arrange
        var refreshTokenValue = "valid-refresh-token";
        var storedToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = "user123",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            TenantId = "tenant123"
        };

        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            TenantId = "tenant123",
            Roles = new List<Role> { Roles.User }
        };

        _refreshTokenRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RefreshToken> { storedToken });

        _userRepository.GetByIdAsync("user123", Arg.Any<CancellationToken>())
            .Returns(user);

        var newRefreshToken = new RefreshToken
        {
            Token = "new-refresh-token",
            UserId = "user123",
            TenantId = "tenant123"
        };

        _refreshTokenRepository.CreateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>())
            .Returns(newRefreshToken);

        // Act
        var result = await _sut.RefreshTokenAsync(refreshTokenValue, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.NotEmpty(result.Token);
        Assert.NotEmpty(result.RefreshToken);
        await _refreshTokenRepository.Received(1).UpdateAsync(
            Arg.Is<RefreshToken>(rt => rt.IsRevoked && rt.RevokedAt != null),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var refreshTokenValue = "invalid-token";

        _refreshTokenRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RefreshToken>());

        // Act
        var result = await _sut.RefreshTokenAsync(refreshTokenValue, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ShouldReturnNull()
    {
        // Arrange
        var refreshTokenValue = "revoked-token";
        var storedToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = "user123",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true, // Already revoked
            RevokedAt = DateTime.UtcNow.AddDays(-1),
            TenantId = "tenant123"
        };

        _refreshTokenRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RefreshToken> { storedToken });

        // Act
        var result = await _sut.RefreshTokenAsync(refreshTokenValue, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldReturnNull()
    {
        // Arrange
        var refreshTokenValue = "expired-token";
        var storedToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = "user123",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            IsRevoked = false,
            TenantId = "tenant123"
        };

        _refreshTokenRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RefreshToken> { storedToken });

        // Act
        var result = await _sut.RefreshTokenAsync(refreshTokenValue, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInactiveUser_ShouldReturnNull()
    {
        // Arrange
        var refreshTokenValue = "valid-token";
        var storedToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = "user123",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            TenantId = "tenant123"
        };

        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            IsActive = false, // Inactive
            TenantId = "tenant123"
        };

        _refreshTokenRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RefreshToken> { storedToken });

        _userRepository.GetByIdAsync("user123", Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _sut.RefreshTokenAsync(refreshTokenValue, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GenerateJwtToken Tests (Internal Method)

    [Fact]
    public void GenerateJwtToken_WithValidUser_ShouldReturnJwtToken()
    {
        // Arrange
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            TenantId = "tenant123",
            Roles = new List<Role> { Roles.User, Roles.ReadOnlyUser }
        };

        // Act
        var token = _sut.GenerateJwtToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains('.', token); // JWT format has dots
    }

    #endregion

    #region GenerateAndSaveRefreshTokenAsync Tests (Internal Method)

    [Fact]
    public async Task GenerateAndSaveRefreshTokenAsync_ShouldGenerateAndSaveToken()
    {
        // Arrange
        var userId = "user123";
        var refreshToken = new RefreshToken
        {
            Token = "generated-token",
            UserId = userId,
            TenantId = userId
        };

        _refreshTokenRepository.CreateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>())
            .Returns(refreshToken);

        // Act
        var token = await _sut.GenerateAndSaveRefreshTokenAsync(userId, CancellationToken.None);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        await _refreshTokenRepository.Received(1).CreateAsync(
            Arg.Is<RefreshToken>(rt =>
                rt.UserId == userId &&
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow
            ),
            Arg.Any<CancellationToken>()
        );
    }

    #endregion
}
