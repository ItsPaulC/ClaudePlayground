using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Domain.Common;
using ClaudePlayground.Domain.Entities;
using ClaudePlayground.Domain.ValueObjects;

namespace ClaudePlayground.Application.Services;

public class BusinessService : IBusinessService
{
    private readonly IRepository<Business> _repository;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;

    public BusinessService(IRepository<Business> repository, ITenantProvider tenantProvider, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _tenantProvider = tenantProvider;
        _currentUserService = currentUserService;
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
}
