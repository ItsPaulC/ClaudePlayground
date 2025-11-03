using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Domain.Common;

namespace ClaudePlayground.Application.Interfaces;

/// <summary>
/// Service interface for user management operations.
/// Handles CRUD operations, tenant isolation, role-based authorization, and user profile management.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves a user by their unique identifier with multi-level authorization checks.
    /// - SuperUsers can view any user across all tenants
    /// - BusinessOwners can view any user within their tenant
    /// - User/ReadOnlyUser roles can only view their own profile
    /// </summary>
    /// <param name="id">The unique identifier of the user</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result containing the user DTO if found and authorized, or an error (NotFound, Forbidden)</returns>
    Task<Result<UserDto>> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current authenticated user's profile based on JWT claims.
    /// Any authenticated user can retrieve their own profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result containing the current user's DTO or an error (Unauthorized, NotFound)</returns>
    Task<Result<UserDto>> GetMeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all users with role-based filtering.
    /// - SuperUsers can see all users across all tenants
    /// - BusinessOwners can see all users within their tenant
    /// - User/ReadOnlyUser roles are not authorized (throws UnauthorizedAccessException)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of all accessible user DTOs</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the current user is not a SuperUser or BusinessOwner</exception>
    Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of users with optional sorting and role-based filtering.
    /// - SuperUsers can see all users across all tenants
    /// - BusinessOwners can see all users within their tenant
    /// - User/ReadOnlyUser roles are not authorized (throws UnauthorizedAccessException)
    /// </summary>
    /// <param name="page">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="sortBy">The property name to sort by (e.g., "Email", "LastName", "CreatedAt"). If null, no sorting is applied</param>
    /// <param name="sortDescending">True for descending sort, false for ascending (default)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>PagedResult containing user DTOs, total count, and pagination metadata</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the current user is not a SuperUser or BusinessOwner</exception>
    Task<PagedResult<UserDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user with role-based authorization and tenant assignment.
    /// - SuperUsers can create users with any role in any tenant (via targetTenantId)
    /// - BusinessOwners can only create User/ReadOnlyUser roles within their own tenant
    /// </summary>
    /// <param name="dto">The user creation data including email, password, name, and roles</param>
    /// <param name="targetTenantId">Optional tenant ID for SuperUsers to create users in specific tenants. BusinessOwners must leave this null.</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result containing the created user DTO or an error (Conflict if email exists, Unauthorized, Validation)</returns>
    Task<Result<UserDto>> CreateAsync(CreateUserDto dto, string? targetTenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user with authorization checks.
    /// - SuperUsers can update any user
    /// - BusinessOwners can only update users within their tenant and can only assign User/ReadOnlyUser roles
    /// </summary>
    /// <param name="id">The unique identifier of the user to update</param>
    /// <param name="dto">The updated user data</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result containing the updated user DTO or an error (NotFound, Unauthorized, Forbidden, Validation)</returns>
    Task<Result<UserDto>> UpdateAsync(string id, UpdateUserDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user with authorization checks.
    /// - SuperUsers can delete any user
    /// - BusinessOwners can only delete users within their tenant
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result indicating success or an error (NotFound, Unauthorized)</returns>
    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
