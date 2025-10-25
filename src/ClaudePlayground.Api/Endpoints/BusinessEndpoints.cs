using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;

namespace ClaudePlayground.Api.Endpoints;

public static class BusinessEndpoints
{
    public static IEndpointRouteBuilder MapBusinessEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/businesses")
            .WithTags("Businesses");

        group.MapGet("/", async (IBusinessService service, CancellationToken ct) =>
        {
            IEnumerable<BusinessDto> businesses = await service.GetAllAsync(ct);
            return Results.Ok(businesses);
        })
        .WithName("GetAllBusinesses")
        .WithOpenApi();

        group.MapGet("/{id}", async (string id, IBusinessService service, CancellationToken ct) =>
        {
            BusinessDto? business = await service.GetByIdAsync(id, ct);
            return business is not null ? Results.Ok(business) : Results.NotFound();
        })
        .WithName("GetBusinessById")
        .WithOpenApi();

        group.MapPost("/", async (CreateBusinessDto dto, IBusinessService service, CancellationToken ct) =>
        {
            BusinessDto business = await service.CreateAsync(dto, ct);
            return Results.Created($"/api/businesses/{business.Id}", business);
        })
        .WithName("CreateBusiness")
        .WithOpenApi();

        group.MapPut("/{id}", async (string id, UpdateBusinessDto dto, IBusinessService service, CancellationToken ct) =>
        {
            try
            {
                BusinessDto business = await service.UpdateAsync(id, dto, ct);
                return Results.Ok(business);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("UpdateBusiness")
        .WithOpenApi();

        group.MapDelete("/{id}", async (string id, IBusinessService service, CancellationToken ct) =>
        {
            bool deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteBusiness")
        .WithOpenApi();

        return app;
    }
}
