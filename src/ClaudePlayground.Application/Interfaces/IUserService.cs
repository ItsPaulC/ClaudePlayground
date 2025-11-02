using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Domain.Common;

namespace ClaudePlayground.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<UserDto?> GetMeAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<UserDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserDto dto, string? targetTenantId = null, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(string id, UpdateUserDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
