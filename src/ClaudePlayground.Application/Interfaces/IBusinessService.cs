using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Domain.Common;

namespace ClaudePlayground.Application.Interfaces;

/// <summary>
/// Service interface for business management operations.
/// Handles CRUD operations, tenant isolation, and role-based authorization for business entities.
/// </summary>
public interface IBusinessService
{
    /// <summary>
    /// Retrieves a business by its unique identifier with tenant isolation.
    /// Non-SuperUser roles can only access businesses within their tenant.
    /// </summary>
    /// <param name="id">The unique identifier of the business</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result containing the business DTO if found and authorized, or an error</returns>
    Task<Result<BusinessDto>> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all businesses with role-based filtering.
    /// SuperUsers can see all businesses across all tenants.
    /// Other roles are limited to their own tenant (enforced by endpoint authorization).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of all accessible business DTOs</returns>
    Task<IEnumerable<BusinessDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of businesses with optional sorting and role-based filtering.
    /// SuperUsers can see all businesses across all tenants.
    /// Other roles are limited to their own tenant.
    /// </summary>
    /// <param name="page">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="sortBy">The property name to sort by (e.g., "Name", "CreatedAt"). If null, no sorting is applied</param>
    /// <param name="sortDescending">True for descending sort, false for ascending (default)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>PagedResult containing business DTOs, total count, and pagination metadata</returns>
    Task<PagedResult<BusinessDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new business. SuperUsers only.
    /// The business is created within the current user's tenant.
    /// </summary>
    /// <param name="dto">The business creation data</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result containing the created business DTO or an error</returns>
    Task<Result<BusinessDto>> CreateAsync(CreateBusinessDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new business along with a BusinessOwner user in an atomic transaction.
    /// SuperUsers only. The business ID becomes the tenant ID, and the user is assigned to that tenant.
    /// This is typically used for initial business onboarding.
    /// </summary>
    /// <param name="dto">The business and user creation data</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result containing the created business, user ID, and authentication tokens, or an error</returns>
    Task<Result<BusinessWithUserDto>> CreateWithUserAsync(CreateBusinessWithUserDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing business with authorization checks.
    /// SuperUsers can update any business.
    /// BusinessOwners can only update businesses within their tenant.
    /// </summary>
    /// <param name="id">The unique identifier of the business to update</param>
    /// <param name="dto">The updated business data</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result containing the updated business DTO or an error (NotFound, Forbidden)</returns>
    Task<Result<BusinessDto>> UpdateAsync(string id, UpdateBusinessDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a business. SuperUsers only (enforced by endpoint authorization).
    /// </summary>
    /// <param name="id">The unique identifier of the business to delete</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result indicating success or an error (NotFound, Forbidden)</returns>
    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
