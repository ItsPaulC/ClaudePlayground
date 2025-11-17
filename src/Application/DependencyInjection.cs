using ClaudePlayground.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudePlayground.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register all validators from this assembly
        services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

        return services;
    }
}
