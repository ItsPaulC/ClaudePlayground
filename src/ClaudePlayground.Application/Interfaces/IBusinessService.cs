using ClaudePlayground.Application.DTOs;

namespace ClaudePlayground.Application.Interfaces;

public interface IBusinessService
{
    Task<BusinessDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<BusinessDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BusinessDto> CreateAsync(CreateBusinessDto dto, CancellationToken cancellationToken = default);
    Task<BusinessDto> UpdateAsync(string id, UpdateBusinessDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
