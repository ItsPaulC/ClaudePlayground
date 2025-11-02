using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Domain.Common;

namespace ClaudePlayground.Application.Interfaces;

public interface IBusinessService
{
    Task<BusinessDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<BusinessDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<BusinessDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<BusinessDto> CreateAsync(CreateBusinessDto dto, CancellationToken cancellationToken = default);
    Task<BusinessWithUserDto> CreateWithUserAsync(CreateBusinessWithUserDto dto, CancellationToken cancellationToken = default);
    Task<BusinessDto> UpdateAsync(string id, UpdateBusinessDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
