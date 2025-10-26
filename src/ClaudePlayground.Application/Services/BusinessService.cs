using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ClaudePlayground.Application.Configuration;
using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Domain.Common;
using ClaudePlayground.Domain.Entities;
using ClaudePlayground.Domain.ValueObjects;
using Microsoft.IdentityModel.Tokens;

namespace ClaudePlayground.Application.Services;

public class BusinessService : IBusinessService
{
    private readonly IRepository<Business> _repository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly JwtSettings _jwtSettings;

    public BusinessService(
        IRepository<Business> repository,
        IRepository<User> userRepository,
        IRepository<RefreshToken> refreshTokenRepository,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUserService,
        JwtSettings jwtSettings)
    {
        _repository = repository;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tenantProvider = tenantProvider;
        _currentUserService = currentUserService;
        _jwtSettings = jwtSettings;
    }

    public async Task<BusinessDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        Business? entity = await _repository.GetByIdAsync(id, cancellationToken);

        // Ensure tenant isolation
        if (entity == null || entity.TenantId != _tenantProvider.GetTenantId())
        {
            return null;
        }

        return MapToDto(entity);
    }

    public async Task<IEnumerable<BusinessDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<Business> entities = await _repository.GetAllAsync(cancellationToken);

        // Super-users can see all businesses (cross-tenant access)
        bool isSuperUser = _currentUserService.IsInRole(Roles.SuperUser);

        if (isSuperUser)
        {
            return entities.Select(MapToDto);
        }

        // Other users filtered by tenant (shouldn't reach here due to endpoint authorization)
        string currentTenantId = _tenantProvider.GetTenantId();
        return entities
            .Where(e => e.TenantId == currentTenantId)
            .Select(MapToDto);
    }

    public async Task<BusinessDto> CreateAsync(CreateBusinessDto dto, CancellationToken cancellationToken = default)
    {
        Business entity = new()
        {
            TenantId = _tenantProvider.GetTenantId(),
            Name = dto.Name,
            Description = dto.Description,
            Address = MapToAddress(dto.Address),
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Website = dto.Website
        };

        Business created = await _repository.CreateAsync(entity, cancellationToken);
        return MapToDto(created);
    }

    public async Task<BusinessWithUserDto> CreateWithUserAsync(CreateBusinessWithUserDto dto, CancellationToken cancellationToken = default)
    {
        // Check if user already exists
        IEnumerable<User> existingUsers = await _userRepository.GetAllAsync(cancellationToken);
        User? existingUser = existingUsers.FirstOrDefault(u => u.Email.Equals(dto.UserEmail, StringComparison.OrdinalIgnoreCase));

        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with email {dto.UserEmail} already exists");
        }

        // Create the business - the business ID will be the tenant ID
        Business business = new()
        {
            Name = dto.Name,
            Description = dto.Description,
            Address = MapToAddress(dto.Address),
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Website = dto.Website
        };

        // Set the business's tenant ID to its own ID (self-referencing)
        business.TenantId = business.Id;

        Business createdBusiness = await _repository.CreateAsync(business, cancellationToken);

        // Create the super-user for this business tenant
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.UserPassword);

        User user = new()
        {
            Email = dto.UserEmail.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = dto.UserFirstName,
            LastName = dto.UserLastName,
            IsActive = true,
            Roles = [Roles.SuperUser],
            TenantId = createdBusiness.Id // User belongs to the business tenant
        };

        User createdUser = await _userRepository.CreateAsync(user, cancellationToken);

        // Generate JWT token
        string token = GenerateJwtToken(createdUser);

        // Generate and save refresh token
        string refreshToken = await GenerateAndSaveRefreshTokenAsync(createdUser.Id, cancellationToken);

        return new BusinessWithUserDto(
            MapToDto(createdBusiness),
            createdUser.Id,
            createdUser.Email,
            token,
            refreshToken
        );
    }

    public async Task<BusinessDto> UpdateAsync(string id, UpdateBusinessDto dto, CancellationToken cancellationToken = default)
    {
        Business? entity = await _repository.GetByIdAsync(id, cancellationToken);

        if (entity == null)
        {
            throw new KeyNotFoundException($"Business with ID {id} not found");
        }

        // Check authorization
        bool isSuperUser = _currentUserService.IsInRole(Roles.SuperUser);
        bool isAdmin = _currentUserService.IsInRole(Roles.Admin);
        string currentTenantId = _tenantProvider.GetTenantId();

        // Super-users can update any business
        // Admins can only update businesses in their own tenant
        if (!isSuperUser && (!isAdmin || entity.TenantId != currentTenantId))
        {
            throw new UnauthorizedAccessException("You do not have permission to update this business");
        }

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Address = MapToAddress(dto.Address);
        entity.PhoneNumber = dto.PhoneNumber;
        entity.Email = dto.Email;
        entity.Website = dto.Website;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        Business updated = await _repository.UpdateAsync(entity, cancellationToken);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        Business? entity = await _repository.GetByIdAsync(id, cancellationToken);

        // Ensure tenant isolation
        if (entity == null || entity.TenantId != _tenantProvider.GetTenantId())
        {
            return false;
        }

        return await _repository.DeleteAsync(id, cancellationToken);
    }

    private static BusinessDto MapToDto(Business entity)
    {
        return new BusinessDto(
            entity.Id,
            entity.TenantId,
            entity.Name,
            entity.Description,
            MapToAddressDto(entity.Address),
            entity.PhoneNumber,
            entity.Email,
            entity.Website,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt
        );
    }

    private static Address? MapToAddress(AddressDto? addressDto)
    {
        if (addressDto == null)
        {
            return null;
        }

        return new Address(
            addressDto.Street,
            addressDto.City,
            addressDto.State,
            addressDto.ZipCode,
            addressDto.Country
        );
    }

    private static AddressDto? MapToAddressDto(Address? address)
    {
        if (address == null)
        {
            return null;
        }

        return new AddressDto(
            address.Street,
            address.City,
            address.State,
            address.ZipCode,
            address.Country
        );
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
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role claims
        foreach (string role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
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
