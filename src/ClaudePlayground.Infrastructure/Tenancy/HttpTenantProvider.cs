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

        // Try to get tenant ID from JWT claim first
        string? tenantFromClaim = httpContext.User?.FindFirst("ten")?.Value;
        if (!string.IsNullOrEmpty(tenantFromClaim))
        {
            return tenantFromClaim;
        }

        // Fall back to header for backward compatibility
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out Microsoft.Extensions.Primitives.StringValues tenantId))
        {
            return tenantId.ToString();
        }

        return string.Empty;
    }
}
