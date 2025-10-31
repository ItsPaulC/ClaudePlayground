using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Domain.Common;
using ClaudePlayground.Domain.Entities;
using ClaudePlayground.Domain.ValueObjects;
using MongoDB.Driver;

namespace ClaudePlayground.Application.Services;

public class UserService : IUserService
{
    private readonly IRepository<User> _repository;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;

    public UserService(
        IRepository<User> repository,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _tenantProvider = tenantProvider;
        _currentUserService = currentUserService;
    }

    public async Task<UserDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        // Retrieve user from database
        User? entity = await _repository.GetByIdAsync(id, cancellationToken);

        if (entity == null)
        {
            return null;
        }

        // Check authorization level
        bool isSuperUser = _currentUserService.IsInRole(Roles.SuperUserValue);
        bool isBusinessOwner = _currentUserService.IsInRole(Roles.BusinessOwnerValue);
        string? currentUserId = _currentUserService.UserId;

        // Super-users can view any user (cross-tenant access)
        if (isSuperUser)
        {
            return MapToDto(entity);
        }

        // Enforce tenant isolation for all non-SuperUsers
        string currentTenantId = _tenantProvider.GetTenantId();

        if (entity.TenantId != currentTenantId)
        {
            // Return null (not found) to avoid leaking information about users in other tenants
            return null;
        }

        // BusinessOwners can view any user in their tenant
        if (isBusinessOwner)
        {
            return MapToDto(entity);
        }

        // User and ReadOnlyUser roles can ONLY view their own user information
        if (entity.Id != currentUserId)
        {
            // Return null (not found) to prevent viewing other users
            return null;
        }

        return MapToDto(entity);
    }

    public async Task<UserDto?> GetMeAsync(CancellationToken cancellationToken = default)
    {
        // Get current user ID from JWT claims
        string? currentUserId = _currentUserService.UserId;

        if (string.IsNullOrEmpty(currentUserId))
        {
            return null; // No user ID in claims
        }

        // Retrieve user from database
        User? entity = await _repository.GetByIdAsync(currentUserId, cancellationToken);

        if (entity == null)
        {
            return null;
        }

        return MapToDto(entity);
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Check authorization - only SuperUser and BusinessOwner can get all users
        bool isSuperUser = _currentUserService.IsInRole(Roles.SuperUserValue);
        bool isBusinessOwner = _currentUserService.IsInRole(Roles.BusinessOwnerValue);

        if (!isSuperUser && !isBusinessOwner)
        {
            throw new UnauthorizedAccessException("Only SuperUser and BusinessOwner can view all users");
        }

        IEnumerable<User> entities = await _repository.GetAllAsync(cancellationToken);

        // Super-users can see all users (cross-tenant access)
        if (isSuperUser)
        {
            return entities.Select(MapToDto);
        }

        // BusinessOwners can only see users in their own tenant
        string currentTenantId = _tenantProvider.GetTenantId();
        return entities
            .Where(e => e.TenantId == currentTenantId)
            .Select(MapToDto);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto, string? targetTenantId = null, CancellationToken cancellationToken = default)
    {
        // Validate authorization
        ValidateUserCreationAuthorization(dto.RoleValues, targetTenantId);

        // Check if user already exists
        IEnumerable<User> existingUsers = await _repository.GetAllAsync(cancellationToken);
        User? existingUser = existingUsers.FirstOrDefault(u => u.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase));

        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with email {dto.Email} already exists");
        }

        // Convert role values to Role objects
        List<Role> roles = new();
        foreach (string roleValue in dto.RoleValues)
        {
            Role? role = Roles.GetByValue(roleValue);
            if (role == null)
            {
                throw new ArgumentException($"Invalid role value: {roleValue}");
            }
            roles.Add(role);
        }

        // Hash the password
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        // Determine the tenant ID for the new user
        string tenantId;
        bool isSuperUser = _currentUserService.IsInRole(Roles.SuperUserValue);

        if (isSuperUser && targetTenantId != null)
        {
            // SuperUser can create users in any tenant
            tenantId = targetTenantId;
        }
        else
        {
            // BusinessOwner creates users in their own tenant
            tenantId = _tenantProvider.GetTenantId();
        }

        // Create new user
        User user = new()
        {
            Email = dto.Email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IsActive = true,
            Roles = roles,
            TenantId = tenantId
        };

        try
        {
            User createdUser = await _repository.CreateAsync(user, cancellationToken);
            return MapToDto(createdUser);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Handle race condition where another request created a user with the same email
            throw new InvalidOperationException($"User with email {dto.Email} already exists");
        }
    }

    public async Task<UserDto> UpdateAsync(string id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        // Check authorization level first
        bool isSuperUser = _currentUserService.IsInRole(Roles.SuperUserValue);
        bool isBusinessOwner = _currentUserService.IsInRole(Roles.BusinessOwnerValue);

        if (!isSuperUser && !isBusinessOwner)
        {
            throw new UnauthorizedAccessException("You do not have permission to update users");
        }

        // Retrieve user from database
        User? entity = await _repository.GetByIdAsync(id, cancellationToken);

        if (entity == null)
        {
            throw new KeyNotFoundException($"User with ID {id} not found");
        }

        // Enforce tenant isolation for non-SuperUsers
        // BusinessOwners can ONLY update users where the user's tenantId matches the JWT's "ten" claim
        if (!isSuperUser)
        {
            string currentTenantId = _tenantProvider.GetTenantId();

            if (entity.TenantId != currentTenantId)
            {
                // Return NotFound instead of Unauthorized to avoid leaking information about users in other tenants
                throw new KeyNotFoundException($"User with ID {id} not found");
            }
        }

        // Validate role changes
        if (!isSuperUser)
        {
            // BusinessOwners can only assign User and ReadOnlyUser roles
            foreach (string roleValue in dto.RoleValues)
            {
                if (roleValue != Roles.UserValue && roleValue != Roles.ReadOnlyUserValue)
                {
                    throw new UnauthorizedAccessException($"You do not have permission to assign the role: {roleValue}");
                }
            }
        }

        // Convert role values to Role objects
        List<Role> roles = new();
        foreach (string roleValue in dto.RoleValues)
        {
            Role? role = Roles.GetByValue(roleValue);
            if (role == null)
            {
                throw new ArgumentException($"Invalid role value: {roleValue}");
            }
            roles.Add(role);
        }

        entity.FirstName = dto.FirstName;
        entity.LastName = dto.LastName;
        entity.IsActive = dto.IsActive;
        entity.Roles = roles;
        entity.UpdatedAt = DateTime.UtcNow;

        User updated = await _repository.UpdateAsync(entity, cancellationToken);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        // Check authorization level first
        bool isSuperUser = _currentUserService.IsInRole(Roles.SuperUserValue);
        bool isBusinessOwner = _currentUserService.IsInRole(Roles.BusinessOwnerValue);

        if (!isSuperUser && !isBusinessOwner)
        {
            return false; // Not authorized to delete users
        }

        // Retrieve user from database
        User? entity = await _repository.GetByIdAsync(id, cancellationToken);

        if (entity == null)
        {
            return false; // User not found
        }

        // Enforce tenant isolation for non-SuperUsers
        // BusinessOwners can ONLY delete users where the user's tenantId matches the JWT's "ten" claim
        if (!isSuperUser)
        {
            string currentTenantId = _tenantProvider.GetTenantId();

            if (entity.TenantId != currentTenantId)
            {
                // Return false (not found) instead of throwing to avoid leaking information
                return false;
            }
        }

        return await _repository.DeleteAsync(id, cancellationToken);
    }

    internal void ValidateUserCreationAuthorization(IEnumerable<string> roleValues, string? targetTenantId)
    {
        bool isSuperUser = _currentUserService.IsInRole(Roles.SuperUserValue);
        bool isBusinessOwner = _currentUserService.IsInRole(Roles.BusinessOwnerValue);

        if (!isSuperUser && !isBusinessOwner)
        {
            throw new UnauthorizedAccessException("You do not have permission to create users");
        }

        // SuperUser can create users with any role
        if (isSuperUser)
        {
            return;
        }

        // BusinessOwner validation
        if (isBusinessOwner)
        {
            // BusinessOwners can only create User and ReadOnlyUser roles
            foreach (string roleValue in roleValues)
            {
                if (roleValue != Roles.UserValue && roleValue != Roles.ReadOnlyUserValue)
                {
                    throw new UnauthorizedAccessException($"BusinessOwners can only create users with User or ReadOnlyUser roles. Cannot assign role: {roleValue}");
                }
            }

            // BusinessOwners can only create users in their own tenant
            string currentTenantId = _tenantProvider.GetTenantId();
            if (targetTenantId != null && targetTenantId != currentTenantId)
            {
                throw new UnauthorizedAccessException("BusinessOwners can only create users in their own tenant");
            }
        }
    }

    private static UserDto MapToDto(User entity)
    {
        return new UserDto(
            entity.Id,
            entity.TenantId,
            entity.Email,
            entity.FirstName,
            entity.LastName,
            entity.IsActive,
            entity.Roles,
            entity.LastLoginAt,
            entity.CreatedAt,
            entity.UpdatedAt
        );
    }
}
