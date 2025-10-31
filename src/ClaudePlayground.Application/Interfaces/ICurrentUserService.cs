namespace ClaudePlayground.Application.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    string TenantId { get; }
    IEnumerable<string> Roles { get; }
    bool IsInRole(string role);
}
