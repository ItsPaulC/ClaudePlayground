using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Domain.Common;
using ClaudePlayground.Domain.Entities;
using ClaudePlayground.Domain.ValueObjects;
using MongoDB.Driver;

namespace ClaudePlayground.Application.Services;

public class BusinessService : IBusinessService
{
    private readonly IRepository<Business> _repository;
    private readonly IRepository<User> _userRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthService _authService;
    private readonly ITransactionManager _transactionManager;

    public BusinessService(
        IRepository<Business> repository,
        IRepository<User> userRepository,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUserService,
        IAuthService authService,
        ITransactionManager transactionManager)
    {
        _repository = repository;
        _userRepository = userRepository;
        _tenantProvider = tenantProvider;
        _currentUserService = currentUserService;
        _authService = authService;
        _transactionManager = transactionManager;
    }

    public async Task<Result<BusinessDto>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        Business? entity = await _repository.GetByIdAsync(id, cancellationToken);

        // Ensure tenant isolation
        if (entity == null || entity.TenantId != _tenantProvider.GetTenantId())
        {
            return Error.NotFound("Business", id);
        }

        return MapToDto(entity);
    }

    public async Task<IEnumerable<BusinessDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<Business> entities = await _repository.GetAllAsync(cancellationToken);

        // Super-users can see all businesses (cross-tenant access)
        bool isSuperUser = _currentUserService.IsInRole(Roles.SuperUserValue);

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

    public async Task<PagedResult<BusinessDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        // Super-users can see all businesses (cross-tenant access)
        bool isSuperUser = _currentUserService.IsInRole(Roles.SuperUserValue);

        PagedResult<Business> pagedEntities;

        if (isSuperUser)
        {
            // Get paginated results for all tenants with sorting
            pagedEntities = await _repository.GetPagedAsync(page, pageSize, sortBy, sortDescending, null, cancellationToken);
        }
        else
        {
            // Get paginated results for current tenant only with sorting
            string currentTenantId = _tenantProvider.GetTenantId();
            pagedEntities = await _repository.GetPagedByTenantAsync(currentTenantId, page, pageSize, sortBy, sortDescending, null, cancellationToken);
        }

        // Map entities to DTOs
        var dtoItems = pagedEntities.Items.Select(MapToDto);

        return new PagedResult<BusinessDto>(
            Items: dtoItems,
            TotalCount: pagedEntities.TotalCount,
            Page: pagedEntities.Page,
            PageSize: pagedEntities.PageSize
        );
    }

    public async Task<Result<BusinessDto>> CreateAsync(CreateBusinessDto dto, CancellationToken cancellationToken = default)
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

    public async Task<Result<BusinessWithUserDto>> CreateWithUserAsync(CreateBusinessWithUserDto dto, CancellationToken cancellationToken = default)
    {
        // Check if user already exists - use efficient query
        User? existingUser = await _userRepository.FindOneAsync(
            u => u.Email.ToLower() == dto.UserEmail.ToLower(),
            cancellationToken);

        if (existingUser != null)
        {
            return Error.Conflict("User.EmailAlreadyExists", $"User with email {dto.UserEmail} already exists");
        }

        // Execute business and user creation within a transaction
        try
        {
            var (createdBusiness, createdUser) = await _transactionManager.ExecuteInTransactionAsync(
                async (session, ct) =>
                {
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

                    // Create business within transaction
                    Business createdBiz = await _repository.CreateWithSessionAsync(business, session, ct);

                    // Create the BusinessOwner for this business tenant
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.UserPassword);

                    User user = new()
                    {
                        Email = dto.UserEmail.ToLowerInvariant(),
                        PasswordHash = passwordHash,
                        FirstName = dto.UserFirstName,
                        LastName = dto.UserLastName,
                        IsActive = true,
                        Roles = [Roles.BusinessOwner],
                        TenantId = createdBiz.Id // User belongs to the business tenant
                    };

                    // Create user within transaction
                    User createdUsr;
                    try
                    {
                        createdUsr = await _userRepository.CreateWithSessionAsync(user, session, ct);
                    }
                    catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                    {
                        // Handle race condition where another request created a user with the same email
                        throw new InvalidOperationException($"User with email {dto.UserEmail} already exists");
                    }

                    return (createdBiz, createdUsr);
                },
                cancellationToken);

            // Generate JWT token using AuthService (after successful transaction commit)
            string token = _authService.GenerateJwtToken(createdUser);

            // Generate and save refresh token using AuthService (outside transaction)
            string refreshToken = await _authService.GenerateAndSaveRefreshTokenAsync(createdUser.Id, cancellationToken);

            return new BusinessWithUserDto(
                MapToDto(createdBusiness),
                createdUser.Id,
                createdUser.Email,
                token,
                refreshToken
            );
        }
        catch (InvalidOperationException ex)
        {
            return Error.Conflict("User.EmailAlreadyExists", ex.Message);
        }
    }

    public async Task<Result<BusinessDto>> UpdateAsync(string id, UpdateBusinessDto dto, CancellationToken cancellationToken = default)
    {
        Business? entity = await _repository.GetByIdAsync(id, cancellationToken);

        if (entity == null)
        {
            return Error.NotFound("Business", id);
        }

        // Check authorization
        bool isSuperUser = _currentUserService.IsInRole(Roles.SuperUserValue);
        bool isBusinessOwner = _currentUserService.IsInRole(Roles.BusinessOwnerValue);
        string currentTenantId = _tenantProvider.GetTenantId();

        // Super-users can update any business
        // BusinessOwners can only update businesses in their own tenant
        if (!isSuperUser && (!isBusinessOwner || entity.TenantId != currentTenantId))
        {
            return Error.Forbidden("You do not have permission to update this business");
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

    public async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        Business? entity = await _repository.GetByIdAsync(id, cancellationToken);

        if (entity == null)
        {
            return Error.NotFound("Business", id);
        }

        // Check authorization
        bool isSuperUser = _currentUserService.IsInRole(Roles.SuperUserValue);
        string currentTenantId = _tenantProvider.GetTenantId();

        // Super-users can delete any business (cross-tenant access)
        // Other users (shouldn't reach here due to endpoint authorization, but enforce anyway)
        if (!isSuperUser && entity.TenantId != currentTenantId)
        {
            return Error.Forbidden("You do not have permission to delete this business");
        }

        bool deleted = await _repository.DeleteAsync(id, cancellationToken);
        return deleted ? Result.Success() : Error.Failure("Business.DeleteFailed", "Failed to delete business");
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
}
