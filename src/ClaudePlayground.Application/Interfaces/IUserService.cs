using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Domain.Common;

namespace ClaudePlayground.Application.Interfaces;

public interface IUserService
{
    Task<Result<UserDto>> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> GetMeAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<UserDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> CreateAsync(CreateUserDto dto, string? targetTenantId = null, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> UpdateAsync(string id, UpdateUserDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
