namespace ClaudePlayground.Domain.Common;

public interface ITenantProvider
{
    string GetTenantId();
}
