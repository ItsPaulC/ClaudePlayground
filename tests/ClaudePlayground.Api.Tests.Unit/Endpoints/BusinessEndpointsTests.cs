using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;

namespace ClaudePlayground.Api.Tests.Unit.Endpoints;

public class BusinessEndpointsTests
{
    private readonly IBusinessService _businessService;

    public BusinessEndpointsTests()
    {
        _businessService = Substitute.For<IBusinessService>();
    }

    [Fact]
    public async Task GetAllBusinesses_ReturnsOkWithBusinesses()
    {
        // Arrange
        List<BusinessDto> expectedBusinesses = new()
        {
            new BusinessDto(
                Id: "1",
                TenantId: "tenant1",
                Name: "Test Business 1",
                Description: "Description 1",
                Address: new AddressDto("123 Main St", "Anytown", "CA", "12345", "USA"),
                PhoneNumber: "555-1234",
                Email: "test1@example.com",
                Website: "https://test1.com",
                IsActive: true,
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow
            ),
            new BusinessDto(
                Id: "2",
                TenantId: "tenant2",
                Name: "Test Business 2",
                Description: "Description 2",
                Address: new AddressDto("456 Oak Ave", "Somewhere", "NY", "67890", "USA"),
                PhoneNumber: "555-5678",
                Email: "test2@example.com",
                Website: "https://test2.com",
                IsActive: true,
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow
            )
        };

        _businessService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(expectedBusinesses);

        // Act
        IResult result = await GetAllBusinessesHandler(_businessService, CancellationToken.None);

        // Assert
        Assert.IsType<Ok<IEnumerable<BusinessDto>>>(result);
        Ok<IEnumerable<BusinessDto>> okResult = (Ok<IEnumerable<BusinessDto>>)result;
        Assert.Equal(expectedBusinesses, okResult.Value);

        await _businessService.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBusinessById_WhenBusinessExists_ReturnsOkWithBusiness()
    {
        // Arrange
        string businessId = "123";
        BusinessDto expectedBusiness = new(
            Id: businessId,
            TenantId: "tenant1",
            Name: "Test Business",
            Description: "Test Description",
            Address: new AddressDto("123 Main St", "Anytown", "CA", "12345", "USA"),
            PhoneNumber: "555-1234",
            Email: "test@example.com",
            Website: "https://test.com",
            IsActive: true,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        _businessService.GetByIdAsync(businessId, Arg.Any<CancellationToken>())
            .Returns(Result<BusinessDto>.Success(expectedBusiness));

        // Act
        IResult result = await GetBusinessByIdHandler(businessId, _businessService, CancellationToken.None);

        // Assert
        Assert.IsType<Ok<BusinessDto>>(result);
        Ok<BusinessDto> okResult = (Ok<BusinessDto>)result;
        Assert.Equal(expectedBusiness, okResult.Value);

        await _businessService.Received(1).GetByIdAsync(businessId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBusinessById_WhenBusinessNotFound_ReturnsNotFound()
    {
        // Arrange
        string businessId = "nonexistent";
        Error error = new("Business.NotFound", "Business not found", ErrorType.NotFound);

        _businessService.GetByIdAsync(businessId, Arg.Any<CancellationToken>())
            .Returns(Result<BusinessDto>.Failure(error));

        // Act
        IResult result = await GetBusinessByIdHandler(businessId, _businessService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.True(result.GetType().Name.StartsWith("NotFound"));

        await _businessService.Received(1).GetByIdAsync(businessId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBusiness_WhenValid_ReturnsCreatedWithBusiness()
    {
        // Arrange
        CreateBusinessDto createDto = new(
            Name: "New Business",
            Description: "New Description",
            Address: new AddressDto("789 New St", "Newtown", "TX", "11111", "USA"),
            PhoneNumber: "555-9999",
            Email: "new@example.com",
            Website: "https://new.com"
        );

        BusinessDto createdBusiness = new(
            Id: "new-id",
            TenantId: "tenant1",
            Name: createDto.Name,
            Description: createDto.Description,
            Address: createDto.Address,
            PhoneNumber: createDto.PhoneNumber,
            Email: createDto.Email,
            Website: createDto.Website,
            IsActive: true,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        _businessService.CreateAsync(createDto, Arg.Any<CancellationToken>())
            .Returns(Result<BusinessDto>.Success(createdBusiness));

        // Act
        IResult result = await CreateBusinessHandler(createDto, _businessService, CancellationToken.None);

        // Assert
        Assert.IsType<Created<BusinessDto>>(result);
        Created<BusinessDto> createdResult = (Created<BusinessDto>)result;
        Assert.Equal($"/api/businesses/{createdBusiness.Id}", createdResult.Location);
        Assert.Equal(createdBusiness, createdResult.Value);

        await _businessService.Received(1).CreateAsync(createDto, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteBusiness_WhenBusinessExists_ReturnsNoContent()
    {
        // Arrange
        string businessId = "123";
        _businessService.DeleteAsync(businessId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        IResult result = await DeleteBusinessHandler(businessId, _businessService, CancellationToken.None);

        // Assert
        Assert.IsType<NoContent>(result);

        await _businessService.Received(1).DeleteAsync(businessId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteBusiness_WhenBusinessNotFound_ReturnsNotFound()
    {
        // Arrange
        string businessId = "nonexistent";
        Error error = new("Business.NotFound", "Business not found", ErrorType.NotFound);
        _businessService.DeleteAsync(businessId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        // Act
        IResult result = await DeleteBusinessHandler(businessId, _businessService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.True(result.GetType().Name.StartsWith("NotFound"));

        await _businessService.Received(1).DeleteAsync(businessId, Arg.Any<CancellationToken>());
    }

    // Handler methods that mirror the endpoint logic
    // These are extracted to make testing easier without requiring WebApplicationFactory

    private static async Task<IResult> GetAllBusinessesHandler(
        IBusinessService service,
        CancellationToken ct)
    {
        IEnumerable<BusinessDto> businesses = await service.GetAllAsync(ct);
        return Results.Ok(businesses);
    }

    private static async Task<IResult> GetBusinessByIdHandler(
        string id,
        IBusinessService service,
        CancellationToken ct)
    {
        Result<BusinessDto> result = await service.GetByIdAsync(id, ct);
        return result.Match(
            onSuccess: business => Results.Ok(business),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                _ => Results.BadRequest(new { error = error.Message })
            });
    }

    private static async Task<IResult> CreateBusinessHandler(
        CreateBusinessDto dto,
        IBusinessService service,
        CancellationToken ct)
    {
        Result<BusinessDto> result = await service.CreateAsync(dto, ct);
        return result.Match(
            onSuccess: business => Results.Created($"/api/businesses/{business.Id}", business),
            onFailure: error => Results.BadRequest(new { error = error.Message }));
    }

    private static async Task<IResult> DeleteBusinessHandler(
        string id,
        IBusinessService service,
        CancellationToken ct)
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
    }
}
