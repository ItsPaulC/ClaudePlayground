using ClaudePlayground.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace ClaudePlayground.Infrastructure.Tenancy;

public class HttpTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetTenantId()
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return string.Empty;
        }

        // SECURITY: Only trust tenant ID from JWT claim
        // Never trust client-provided headers as they can be spoofed
        string? tenantFromClaim = httpContext.User?.FindFirst("ten")?.Value;

        return tenantFromClaim ?? string.Empty;
    }
}
