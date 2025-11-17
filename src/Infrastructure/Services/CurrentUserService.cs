using System.Security.Claims;
using ClaudePlayground.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ClaudePlayground.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

    public string TenantId
    {
        get
        {
            // SECURITY: Only trust tenant ID from JWT claim
            // Never trust client-provided headers as they can be spoofed
            string? tenantFromClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("ten")?.Value;
            return tenantFromClaim ?? string.Empty;
        }
    }

    public IEnumerable<string> Roles
    {
        get
        {
            ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
            {
                return Enumerable.Empty<string>();
            }

            return user.FindAll(ClaimTypes.Role).Select(c => c.Value);
        }
    }

    public bool IsInRole(string role)
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
    }
}
