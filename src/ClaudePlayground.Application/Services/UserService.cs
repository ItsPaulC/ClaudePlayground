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

    public async Task<Result<UserDto>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        // Retrieve user from database
        User? entity = await _repository.GetByIdAsync(id, cancellationToken);

        if (entity == null)
        {
            return Error.NotFound("User", id);
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
            // Return NotFound to avoid leaking information about users in other tenants
            return Error.NotFound("User", id);
        }

        // BusinessOwners can view any user in their tenant
        if (isBusinessOwner)
        {
            return MapToDto(entity);
        }

        // User and ReadOnlyUser roles can ONLY view their own user information
        if (entity.Id != currentUserId)
        {
            // Return Forbidden to prevent viewing other users
            return Error.Forbidden("You can only view your own user information");
        }

        return MapToDto(entity);
    }

    public async Task<Result<UserDto>> GetMeAsync(CancellationToken cancellationToken = default)
    {
        // Get current user ID from JWT claims
        string? currentUserId = _currentUserService.UserId;

        if (string.IsNullOrEmpty(currentUserId))
        {
            return Error.Unauthorized("User ID not found in authentication token");
        }

        // Retrieve user from database
        User? entity = await _repository.GetByIdAsync(currentUserId, cancellationToken);

        if (entity == null)
        {
            return Error.NotFound("User", currentUserId);
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

    public async Task<PagedResult<UserDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Check authorization - only SuperUser and BusinessOwner can get paginated users
        bool isSuperUser = _currentUserService.IsInRole(Roles.SuperUserValue);
        bool isBusinessOwner = _currentUserService.IsInRole(Roles.BusinessOwnerValue);

        if (!isSuperUser && !isBusinessOwner)
        {
            throw new UnauthorizedAccessException("Only SuperUser and BusinessOwner can view all users");
        }

        PagedResult<User> pagedEntities;

        if (isSuperUser)
        {
            // Get paginated results for all tenants
            pagedEntities = await _repository.GetPagedAsync(page, pageSize, cancellationToken);
        }
        else
        {
            // Get paginated results for current tenant only
            string currentTenantId = _tenantProvider.GetTenantId();
            pagedEntities = await _repository.GetPagedByTenantAsync(currentTenantId, page, pageSize, cancellationToken);
        }

        // Map entities to DTOs
        var dtoItems = pagedEntities.Items.Select(MapToDto);

        return new PagedResult<UserDto>(
            Items: dtoItems,
            TotalCount: pagedEntities.TotalCount,
            Page: pagedEntities.Page,
            PageSize: pagedEntities.PageSize
        );
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserDto dto, string? targetTenantId = null, CancellationToken cancellationToken = default)
    {
        // Validate authorization
        try
        {
            ValidateUserCreationAuthorization(dto.RoleValues, targetTenantId);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Error.Unauthorized(ex.Message);
        }

        // Check if user already exists - use efficient query
        User? existingUser = await _repository.FindOneAsync(
            u => u.Email.ToLower() == dto.Email.ToLower(),
            cancellationToken);

        if (existingUser != null)
        {
            return Error.Conflict("User.EmailAlreadyExists", $"User with email {dto.Email} already exists");
        }

        // Convert role values to Role objects
        List<Role> roles = new();
        foreach (string roleValue in dto.RoleValues)
        {
            Role? role = Roles.GetByValue(roleValue);
            if (role == null)
            {
                return Error.Validation("User.InvalidRole", $"Invalid role value: {roleValue}");
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
            return Error.Conflict("User.EmailAlreadyExists", $"User with email {dto.Email} already exists");
        }
    }

    public async Task<Result<UserDto>> UpdateAsync(string id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        // Check authorization level first
        bool isSuperUser = _currentUserService.IsInRole(Roles.SuperUserValue);
        bool isBusinessOwner = _currentUserService.IsInRole(Roles.BusinessOwnerValue);

        if (!isSuperUser && !isBusinessOwner)
        {
            return Error.Unauthorized("You do not have permission to update users");
        }

        // Retrieve user from database
        User? entity = await _repository.GetByIdAsync(id, cancellationToken);

        if (entity == null)
        {
            return Error.NotFound("User", id);
        }

        // Enforce tenant isolation for non-SuperUsers
        // BusinessOwners can ONLY update users where the user's tenantId matches the JWT's "ten" claim
        if (!isSuperUser)
        {
            string currentTenantId = _tenantProvider.GetTenantId();

            if (entity.TenantId != currentTenantId)
            {
                // Return NotFound instead of Forbidden to avoid leaking information about users in other tenants
                return Error.NotFound("User", id);
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
                    return Error.Forbidden($"You do not have permission to assign the role: {roleValue}");
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
                return Error.Validation("User.InvalidRole", $"Invalid role value: {roleValue}");
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

    public async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        // Check authorization level first
        bool isSuperUser = _currentUserService.IsInRole(Roles.SuperUserValue);
        bool isBusinessOwner = _currentUserService.IsInRole(Roles.BusinessOwnerValue);

        if (!isSuperUser && !isBusinessOwner)
        {
            return Error.Unauthorized("You do not have permission to delete users");
        }

        // Retrieve user from database
        User? entity = await _repository.GetByIdAsync(id, cancellationToken);

        if (entity == null)
        {
            return Error.NotFound("User", id);
        }

        // Enforce tenant isolation for non-SuperUsers
        // BusinessOwners can ONLY delete users where the user's tenantId matches the JWT's "ten" claim
        if (!isSuperUser)
        {
            string currentTenantId = _tenantProvider.GetTenantId();

            if (entity.TenantId != currentTenantId)
            {
                // Return NotFound instead of Forbidden to avoid leaking information
                return Error.NotFound("User", id);
            }
        }

        bool deleted = await _repository.DeleteAsync(id, cancellationToken);
        return deleted ? Result.Success() : Error.Failure("User.DeleteFailed", "Failed to delete user");
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
