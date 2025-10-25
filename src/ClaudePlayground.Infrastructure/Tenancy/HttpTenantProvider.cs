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

        // Try to get tenant ID from header
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out Microsoft.Extensions.Primitives.StringValues tenantId))
        {
            return tenantId.ToString();
        }

        // Could also check query string, claims, etc.
        return string.Empty;
    }
}
