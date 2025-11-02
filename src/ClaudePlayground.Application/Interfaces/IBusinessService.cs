using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Domain.Common;

namespace ClaudePlayground.Application.Interfaces;

public interface IBusinessService
{
    Task<Result<BusinessDto>> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<BusinessDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<BusinessDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default);
    Task<Result<BusinessDto>> CreateAsync(CreateBusinessDto dto, CancellationToken cancellationToken = default);
    Task<Result<BusinessWithUserDto>> CreateWithUserAsync(CreateBusinessWithUserDto dto, CancellationToken cancellationToken = default);
    Task<Result<BusinessDto>> UpdateAsync(string id, UpdateBusinessDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
