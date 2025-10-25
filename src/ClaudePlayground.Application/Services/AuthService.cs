using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClaudePlayground.Application.Configuration;
using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Domain.Common;
using ClaudePlayground.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace ClaudePlayground.Application.Services;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly JwtSettings _jwtSettings;

    public AuthService(IRepository<User> userRepository, JwtSettings jwtSettings)
    {
        _userRepository = userRepository;
        _jwtSettings = jwtSettings;
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

        // Create new user
        User user = new()
        {
            Email = registerDto.Email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            IsActive = true,
            TenantId = registerDto.Email.ToLowerInvariant() // Use email as tenant ID for now
        };

        User createdUser = await _userRepository.CreateAsync(user, ct);

        // Generate JWT token
        string token = GenerateJwtToken(createdUser);

        return new AuthResponseDto(
            token,
            createdUser.Email,
            createdUser.FirstName,
            createdUser.LastName
        );
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

        if (!user.IsActive)
        {
            return null; // User is not active
        }

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, ct);

        // Generate JWT token
        string token = GenerateJwtToken(user);

        return new AuthResponseDto(
            token,
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
            user.Email,
            user.FirstName,
            user.LastName,
            user.IsActive,
            user.LastLoginAt,
            user.CreatedAt
        );
    }

    private string GenerateJwtToken(User user)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        Claim[] claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        JwtSecurityToken token = new(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
