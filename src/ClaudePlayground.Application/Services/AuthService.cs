using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ClaudePlayground.Application.Configuration;
using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Domain.Common;
using ClaudePlayground.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

namespace ClaudePlayground.Application.Services;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly JwtSettings _jwtSettings;
    private readonly IEmailService _emailService;

    public AuthService(
        IRepository<User> userRepository,
        IRepository<RefreshToken> refreshTokenRepository,
        JwtSettings jwtSettings,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtSettings = jwtSettings;
        _emailService = emailService;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto, CancellationToken ct = default)
    {
        // Check if user already exists
        IEnumerable<User> existingUsers = await _userRepository.GetAllAsync(ct);
        User? existingUser = existingUsers.FirstOrDefault(u => u.Email.Equals(registerDto.Email, StringComparison.OrdinalIgnoreCase));

        if (existingUser != null)
        {
            return null; // User already exists
        }

        // Hash the password
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

        // Generate email verification token
        string verificationToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        // Create new user
        User user = new()
        {
            Email = registerDto.Email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            IsActive = true,
            IsEmailVerified = false,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24), // Token expires in 24 hours
            Roles = registerDto.Roles ?? [],
            TenantId = registerDto.Email.ToLowerInvariant() // Use email as tenant ID for now
        };

        User createdUser;
        try
        {
            createdUser = await _userRepository.CreateAsync(user, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Handle race condition where another request created a user with the same email
            return null; // User already exists
        }

        // Send verification email
        await _emailService.SendEmailVerificationAsync(createdUser.Email, verificationToken, ct);

        // Return null - user must verify email before they can login
        // The calling endpoint should return a message instructing user to check their email
        return null;
    }

    public async Task<bool> VerifyEmailAsync(string token, CancellationToken ct = default)
    {
        // Find user by verification token
        IEnumerable<User> users = await _userRepository.GetAllAsync(ct);
        User? user = users.FirstOrDefault(u => u.EmailVerificationToken == token);

        if (user == null)
        {
            return false; // Invalid token
        }

        // Check if token has expired
        if (user.EmailVerificationTokenExpiresAt == null || user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
        {
            return false; // Token expired
        }

        // Check if email is already verified
        if (user.IsEmailVerified)
        {
            return true; // Already verified
        }

        // Mark email as verified and clear the token
        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, ct);

        return true;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto, CancellationToken ct = default)
    {
        // Find user by email
        IEnumerable<User> users = await _userRepository.GetAllAsync(ct);
        User? user = users.FirstOrDefault(u => u.Email.Equals(loginDto.Email, StringComparison.OrdinalIgnoreCase));

        if (user == null)
        {
            return null; // User not found
        }

        // Verify password
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            return null; // Invalid password
        }

        // Check if email is verified
        if (!user.IsEmailVerified)
        {
            return null; // Email not verified
        }

        if (!user.IsActive)
        {
            return null; // User is not active
        }

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, ct);

        // Generate JWT token
        string token = GenerateJwtToken(user);

        // Generate and save refresh token
        string refreshTokenValue = await GenerateAndSaveRefreshTokenAsync(user.Id, ct);

        return new AuthResponseDto(
            token,
            refreshTokenValue,
            user.TenantId,
            user.Email,
            user.FirstName,
            user.LastName
        );
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken ct = default)
    {
        IEnumerable<User> users = await _userRepository.GetAllAsync(ct);
        User? user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        if (user == null)
        {
            return null;
        }

        return new UserDto(
            user.Id,
            user.TenantId,
            user.Email,
            user.FirstName,
            user.LastName,
            user.IsActive,
            user.Roles,
            user.LastLoginAt,
            user.CreatedAt,
            user.UpdatedAt
        );
    }

    public async Task<bool> ChangePasswordAsync(string email, ChangePasswordDto changePasswordDto, CancellationToken ct = default)
    {
        // Find user by email
        IEnumerable<User> users = await _userRepository.GetAllAsync(ct);
        User? user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        if (user == null)
        {
            return false; // User not found
        }

        // Verify current password
        bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash);

        if (!isCurrentPasswordValid)
        {
            return false; // Current password is incorrect
        }

        // Hash new password
        string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);

        // Update password
        user.PasswordHash = newPasswordHash;
        await _userRepository.UpdateAsync(user, ct);

        return true;
    }

    private string GenerateJwtToken(User user)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = new()
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("ten", user.TenantId)
        };

        // Add role claims
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Value));
        }

        JwtSecurityToken token = new(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        // Find refresh token in database
        IEnumerable<RefreshToken> refreshTokens = await _refreshTokenRepository.GetAllAsync(ct);
        RefreshToken? storedToken = refreshTokens.FirstOrDefault(rt => rt.Token == refreshToken);

        if (storedToken == null)
        {
            return null; // Refresh token not found
        }

        // Check if token is revoked
        if (storedToken.IsRevoked)
        {
            return null; // Token has been revoked
        }

        // Check if token is expired
        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return null; // Token has expired
        }

        // Get user
        User? user = await _userRepository.GetByIdAsync(storedToken.UserId, ct);

        if (user == null || !user.IsActive)
        {
            return null; // User not found or inactive
        }

        // Revoke old refresh token
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(storedToken, ct);

        // Generate new JWT token
        string newJwtToken = GenerateJwtToken(user);

        // Generate new refresh token
        string newRefreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id, ct);

        return new AuthResponseDto(
            newJwtToken,
            newRefreshToken,
            user.TenantId,
            user.Email,
            user.FirstName,
            user.LastName
        );
    }

    private async Task<string> GenerateAndSaveRefreshTokenAsync(string userId, CancellationToken ct = default)
    {
        // Generate cryptographically secure random token
        byte[] randomBytes = new byte[64];
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        string token = Convert.ToBase64String(randomBytes);

        // Create refresh token entity
        RefreshToken refreshToken = new()
        {
            Token = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            IsRevoked = false,
            TenantId = userId // Using userId as tenant for now
        };

        // Save to database
        await _refreshTokenRepository.CreateAsync(refreshToken, ct);

        return token;
    }
}
