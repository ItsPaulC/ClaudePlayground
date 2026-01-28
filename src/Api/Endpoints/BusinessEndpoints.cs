using Asp.Versioning;
using Asp.Versioning.Builder;
using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Domain.Common;

namespace ClaudePlayground.Api.Endpoints;

public static class BusinessEndpoints
{
    // Whitelist of allowed sort fields to prevent NoSQL injection
    private static readonly HashSet<string> AllowedBusinessSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "id",
        "name",
        "email",
        "phonenumber",
        "createdat",
        "updatedat"
    };

    public static IEndpointRouteBuilder MapBusinessEndpoints(this IEndpointRouteBuilder app)
    {
        ApiVersionSet apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        RouteGroupBuilder group = app.MapGroup("/api/v{version:apiVersion}/businesses")
            .WithApiVersionSet(apiVersionSet)
            .WithTags("Businesses")
            .RequireAuthorization();

        // Get All Businesses - Super-user only (cross-tenant access)
        group.MapGet("/", async (IBusinessService service, CancellationToken ct) =>
        {
            IEnumerable<BusinessDto> businesses = await service.GetAllAsync(ct);
            return Results.Ok(businesses);
        })
        .WithName("GetAllBusinesses")
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue));

        // Get Paginated Businesses - Authenticated users (tenant-scoped for non-SuperUsers)
        group.MapGet("/paged", async (
            int page,
            int pageSize,
            string? sortBy,
            bool sortDescending,
            IBusinessService service,
            CancellationToken ct) =>
        {
            // Validate pagination parameters
            if (page < 1)
            {
                return Results.BadRequest(new { error = "Page must be greater than or equal to 1" });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return Results.BadRequest(new { error = "PageSize must be between 1 and 100" });
            }

            // Validate sortBy parameter to prevent NoSQL injection
            if (!string.IsNullOrEmpty(sortBy) && !AllowedBusinessSortFields.Contains(sortBy))
            {
                return Results.BadRequest(new { error = $"Invalid sort field. Allowed fields: {string.Join(", ", AllowedBusinessSortFields)}" });
            }

            PagedResult<BusinessDto> businesses = await service.GetPagedAsync(page, pageSize, sortBy, sortDescending, ct);
            return Results.Ok(businesses);
        })
        .WithName("GetPagedBusinesses");

        // Get Business by ID - Authenticated users (tenant-scoped)
        group.MapGet("/{id}", async (string id, IBusinessService service, CancellationToken ct) =>
        {
            Result<BusinessDto> result = await service.GetByIdAsync(id, ct);
            return result.Match(
                onSuccess: business => Results.Ok(business),
                onFailure: error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                    _ => Results.BadRequest(new { error = error.Message })
                });
        })
        .WithName("GetBusinessById");

        app.MapPost("/api/v{version:apiVersion}/businesses/with-user", async (CreateBusinessWithUserDto dto, IBusinessService service, CancellationToken ct) =>
        {
            Result<BusinessWithUserDto> result = await service.CreateWithUserAsync(dto, ct);
            return result.Match(
                onSuccess: businessWithUser => Results.Created($"/api/v1.0/businesses/{businessWithUser.Business.Id}", businessWithUser),
                onFailure: error => error.Type switch
                {
                    ErrorType.Conflict => Results.Conflict(new { error = error.Message }),
                    ErrorType.Validation => Results.BadRequest(new { error = error.Message }),
                    _ => Results.BadRequest(new { error = error.Message })
                });
        })
        .WithName("CreateBusinessWithUser")
        .WithApiVersionSet(apiVersionSet)
        .WithTags("Businesses")
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue));

        // Create Business - Super-user only
        group.MapPost("/", async (CreateBusinessDto dto, IBusinessService service, CancellationToken ct) =>
        {
            Result<BusinessDto> result = await service.CreateAsync(dto, ct);
            return result.Match(
                onSuccess: business => Results.Created($"/api/v1.0/businesses/{business.Id}", business),
                onFailure: error => Results.BadRequest(new { error = error.Message }));
        })
        .WithName("CreateBusiness")
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue));

        // Update Business - Super-user (any) or BusinessOwner (own tenant only)
        group.MapPut("/{id}", async (string id, UpdateBusinessDto dto, IBusinessService service, CancellationToken ct) =>
        {
            Result<BusinessDto> result = await service.UpdateAsync(id, dto, ct);
            return result.Match(
                onSuccess: business => Results.Ok(business),
                onFailure: error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                    ErrorType.Forbidden => Results.Forbid(),
                    _ => Results.BadRequest(new { error = error.Message })
                });
        })
        .WithName("UpdateBusiness")
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue, Roles.BusinessOwnerValue));

        // Delete Business - Super-user only
        group.MapDelete("/{id}", async (string id, IBusinessService service, CancellationToken ct) =>
        {
            Result result = await service.DeleteAsync(id, ct);
            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                    ErrorType.Forbidden => Results.Forbid(),
                    _ => Results.BadRequest(new { error = error.Message })
                });
        })
        .WithName("DeleteBusiness")
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue));

        return app;
    }
}
