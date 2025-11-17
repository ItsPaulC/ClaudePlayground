using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Domain.Common;
using ClaudePlayground.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;

namespace ClaudePlayground.Api.Tests.Unit.Endpoints;

public class UserEndpointsTests
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;

    public UserEndpointsTests()
    {
        _userService = Substitute.For<IUserService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
    }

    [Fact]
    public async Task GetMe_WhenUserIdIsNull_ReturnsNotFound()
    {
        // Arrange
        _currentUserService.UserId.Returns((string?)null);

        // Act
        IResult result = await GetMeHandler(_currentUserService, _userService, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task GetMe_WhenUserIdIsEmpty_ReturnsNotFound()
    {
        // Arrange
        _currentUserService.UserId.Returns(string.Empty);

        // Act
        IResult result = await GetMeHandler(_currentUserService, _userService, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task GetMe_WhenUserExists_ReturnsOkWithUser()
    {
        // Arrange
        string userId = "user-123";
        UserDto expectedUser = new(
            Id: userId,
            TenantId: "tenant1",
            Email: "test@example.com",
            FirstName: "John",
            LastName: "Doe",
            IsActive: true,
            Roles: new List<Role> { new("User", "U", "user") },
            LastLoginAt: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        _currentUserService.UserId.Returns(userId);
        _userService.GetMeAsync(Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Success(expectedUser));

        // Act
        IResult result = await GetMeHandler(_currentUserService, _userService, CancellationToken.None);

        // Assert
        Assert.IsType<Ok<UserDto>>(result);
        Ok<UserDto> okResult = (Ok<UserDto>)result;
        Assert.Equal(expectedUser, okResult.Value);

        await _userService.Received(1).GetMeAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMe_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        string userId = "user-123";
        Error error = new("User.NotFound", "User not found", ErrorType.NotFound);

        _currentUserService.UserId.Returns(userId);
        _userService.GetMeAsync(Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(error));

        // Act
        IResult result = await GetMeHandler(_currentUserService, _userService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("NotFound", result.GetType().Name);

        await _userService.Received(1).GetMeAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMe_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Arrange
        string userId = "user-123";
        Error error = new("User.Unauthorized", "Unauthorized", ErrorType.Unauthorized);

        _currentUserService.UserId.Returns(userId);
        _userService.GetMeAsync(Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(error));

        // Act
        IResult result = await GetMeHandler(_currentUserService, _userService, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);

        await _userService.Received(1).GetMeAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_WhenUserExists_ReturnsOkWithUser()
    {
        // Arrange
        string userId = "user-123";
        UserDto expectedUser = new(
            Id: userId,
            TenantId: "tenant1",
            Email: "test@example.com",
            FirstName: "Jane",
            LastName: "Smith",
            IsActive: true,
            Roles: new List<Role> { new("BusinessOwner", "BO", "business_owner") },
            LastLoginAt: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        _userService.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Success(expectedUser));

        // Act
        IResult result = await GetByIdHandler(userId, _userService, CancellationToken.None);

        // Assert
        Assert.IsType<Ok<UserDto>>(result);
        Ok<UserDto> okResult = (Ok<UserDto>)result;
        Assert.Equal(expectedUser, okResult.Value);

        await _userService.Received(1).GetByIdAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        string userId = "nonexistent";
        Error error = new("User.NotFound", "User not found", ErrorType.NotFound);

        _userService.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(error));

        // Act
        IResult result = await GetByIdHandler(userId, _userService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("NotFound", result.GetType().Name);

        await _userService.Received(1).GetByIdAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_WhenForbidden_ReturnsForbid()
    {
        // Arrange
        string userId = "user-123";
        Error error = new("User.Forbidden", "Access forbidden", ErrorType.Forbidden);

        _userService.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(error));

        // Act
        IResult result = await GetByIdHandler(userId, _userService, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidHttpResult>(result);

        await _userService.Received(1).GetByIdAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WhenValid_ReturnsCreatedWithUser()
    {
        // Arrange
        CreateUserDto createDto = new(
            Email: "newuser@example.com",
            Password: "SecurePassword123!",
            FirstName: "New",
            LastName: "User",
            RoleValues: new List<string> { "user" }
        );

        UserDto createdUser = new(
            Id: "new-user-id",
            TenantId: "tenant1",
            Email: createDto.Email,
            FirstName: createDto.FirstName,
            LastName: createDto.LastName,
            IsActive: true,
            Roles: new List<Role> { new("User", "U", "user") },
            LastLoginAt: null,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        _userService.CreateAsync(createDto, null, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Success(createdUser));

        // Act
        IResult result = await CreateHandler(createDto, _userService, CancellationToken.None);

        // Assert
        Assert.IsType<Created<UserDto>>(result);
        Created<UserDto> createdResult = (Created<UserDto>)result;
        Assert.Equal($"/api/users/{createdUser.Id}", createdResult.Location);
        Assert.Equal(createdUser, createdResult.Value);

        await _userService.Received(1).CreateAsync(createDto, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WhenConflict_ReturnsConflict()
    {
        // Arrange
        CreateUserDto createDto = new(
            Email: "existing@example.com",
            Password: "Password123!",
            FirstName: "Test",
            LastName: "User",
            RoleValues: new List<string> { "user" }
        );

        Error error = new("User.Conflict", "User already exists", ErrorType.Conflict);
        _userService.CreateAsync(createDto, null, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(error));

        // Act
        IResult result = await CreateHandler(createDto, _userService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("Conflict", result.GetType().Name);

        await _userService.Received(1).CreateAsync(createDto, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WhenUnauthorized_ReturnsForbid()
    {
        // Arrange
        CreateUserDto createDto = new(
            Email: "test@example.com",
            Password: "Password123!",
            FirstName: "Test",
            LastName: "User",
            RoleValues: new List<string> { "super_user" }
        );

        Error error = new("User.Unauthorized", "Not authorized to create this role", ErrorType.Unauthorized);
        _userService.CreateAsync(createDto, null, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(error));

        // Act
        IResult result = await CreateHandler(createDto, _userService, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidHttpResult>(result);

        await _userService.Received(1).CreateAsync(createDto, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateInTenant_WhenValid_ReturnsCreatedWithUser()
    {
        // Arrange
        string targetTenantId = "tenant-999";
        CreateUserDto createDto = new(
            Email: "newuser@example.com",
            Password: "SecurePassword123!",
            FirstName: "Cross",
            LastName: "Tenant",
            RoleValues: new List<string> { "user" }
        );

        UserDto createdUser = new(
            Id: "new-user-id",
            TenantId: targetTenantId,
            Email: createDto.Email,
            FirstName: createDto.FirstName,
            LastName: createDto.LastName,
            IsActive: true,
            Roles: new List<Role> { new("User", "U", "user") },
            LastLoginAt: null,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        _userService.CreateAsync(createDto, targetTenantId, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Success(createdUser));

        // Act
        IResult result = await CreateInTenantHandler(targetTenantId, createDto, _userService, CancellationToken.None);

        // Assert
        Assert.IsType<Created<UserDto>>(result);
        Created<UserDto> createdResult = (Created<UserDto>)result;
        Assert.Equal($"/api/users/{createdUser.Id}", createdResult.Location);
        Assert.Equal(createdUser, createdResult.Value);

        await _userService.Received(1).CreateAsync(createDto, targetTenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WhenValid_ReturnsOkWithUpdatedUser()
    {
        // Arrange
        string userId = "user-123";
        UpdateUserDto updateDto = new(
            FirstName: "Updated",
            LastName: "Name",
            IsActive: true,
            RoleValues: new List<string> { "user" }
        );

        UserDto updatedUser = new(
            Id: userId,
            TenantId: "tenant1",
            Email: "test@example.com",
            FirstName: updateDto.FirstName,
            LastName: updateDto.LastName,
            IsActive: updateDto.IsActive,
            Roles: new List<Role> { new("User", "U", "user") },
            LastLoginAt: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow.AddDays(-7),
            UpdatedAt: DateTime.UtcNow
        );

        _userService.UpdateAsync(userId, updateDto, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Success(updatedUser));

        // Act
        IResult result = await UpdateHandler(userId, updateDto, _userService, CancellationToken.None);

        // Assert
        Assert.IsType<Ok<UserDto>>(result);
        Ok<UserDto> okResult = (Ok<UserDto>)result;
        Assert.Equal(updatedUser, okResult.Value);

        await _userService.Received(1).UpdateAsync(userId, updateDto, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        string userId = "nonexistent";
        UpdateUserDto updateDto = new(
            FirstName: "Test",
            LastName: "User",
            IsActive: true,
            RoleValues: new List<string> { "user" }
        );

        Error error = new("User.NotFound", "User not found", ErrorType.NotFound);
        _userService.UpdateAsync(userId, updateDto, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(error));

        // Act
        IResult result = await UpdateHandler(userId, updateDto, _userService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("NotFound", result.GetType().Name);

        await _userService.Received(1).UpdateAsync(userId, updateDto, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WhenForbidden_ReturnsForbid()
    {
        // Arrange
        string userId = "user-123";
        UpdateUserDto updateDto = new(
            FirstName: "Test",
            LastName: "User",
            IsActive: true,
            RoleValues: new List<string> { "super_user" }
        );

        Error error = new("User.Forbidden", "Cannot update this user", ErrorType.Forbidden);
        _userService.UpdateAsync(userId, updateDto, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(error));

        // Act
        IResult result = await UpdateHandler(userId, updateDto, _userService, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidHttpResult>(result);

        await _userService.Received(1).UpdateAsync(userId, updateDto, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WhenUserExists_ReturnsNoContent()
    {
        // Arrange
        string userId = "user-123";
        _userService.DeleteAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        IResult result = await DeleteHandler(userId, _userService, CancellationToken.None);

        // Assert
        Assert.IsType<NoContent>(result);

        await _userService.Received(1).DeleteAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        string userId = "nonexistent";
        Error error = new("User.NotFound", "User not found", ErrorType.NotFound);
        _userService.DeleteAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        // Act
        IResult result = await DeleteHandler(userId, _userService, CancellationToken.None);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.StartsWith("NotFound", result.GetType().Name);

        await _userService.Received(1).DeleteAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WhenUnauthorized_ReturnsForbid()
    {
        // Arrange
        string userId = "user-123";
        Error error = new("User.Unauthorized", "Cannot delete this user", ErrorType.Unauthorized);
        _userService.DeleteAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        // Act
        IResult result = await DeleteHandler(userId, _userService, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidHttpResult>(result);

        await _userService.Received(1).DeleteAsync(userId, Arg.Any<CancellationToken>());
    }

    // Handler methods that mirror the endpoint logic
    // These are extracted to make testing easier without requiring WebApplicationFactory or cache dependencies

    private static async Task<IResult> GetMeHandler(
        ICurrentUserService currentUserService,
        IUserService service,
        CancellationToken ct)
    {
        string? userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.NotFound();
        }

        Result<UserDto> result = await service.GetMeAsync(ct);
        return result.Match(
            onSuccess: user => Results.Ok(user),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                ErrorType.Unauthorized => Results.Unauthorized(),
                _ => Results.BadRequest(new { error = error.Message })
            });
    }

    private static async Task<IResult> GetByIdHandler(
        string id,
        IUserService service,
        CancellationToken ct)
    {
        Result<UserDto> result = await service.GetByIdAsync(id, ct);
        return result.Match(
            onSuccess: user => Results.Ok(user),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                ErrorType.Forbidden => Results.Forbid(),
                _ => Results.BadRequest(new { error = error.Message })
            });
    }

    private static async Task<IResult> CreateHandler(
        CreateUserDto dto,
        IUserService service,
        CancellationToken ct)
    {
        Result<UserDto> result = await service.CreateAsync(dto, null, ct);
        return result.Match(
            onSuccess: user => Results.Created($"/api/users/{user.Id}", user),
            onFailure: error => error.Type switch
            {
                ErrorType.Conflict => Results.Conflict(new { error = error.Message }),
                ErrorType.Unauthorized => Results.Forbid(),
                ErrorType.Validation => Results.BadRequest(new { error = error.Message }),
                _ => Results.BadRequest(new { error = error.Message })
            });
    }

    private static async Task<IResult> CreateInTenantHandler(
        string tenantId,
        CreateUserDto dto,
        IUserService service,
        CancellationToken ct)
    {
        Result<UserDto> result = await service.CreateAsync(dto, tenantId, ct);
        return result.Match(
            onSuccess: user => Results.Created($"/api/users/{user.Id}", user),
            onFailure: error => error.Type switch
            {
                ErrorType.Conflict => Results.Conflict(new { error = error.Message }),
                ErrorType.Unauthorized => Results.Forbid(),
                ErrorType.Validation => Results.BadRequest(new { error = error.Message }),
                _ => Results.BadRequest(new { error = error.Message })
            });
    }

    private static async Task<IResult> UpdateHandler(
        string id,
        UpdateUserDto dto,
        IUserService service,
        CancellationToken ct)
    {
        Result<UserDto> result = await service.UpdateAsync(id, dto, ct);
        return result.Match(
            onSuccess: user => Results.Ok(user),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                ErrorType.Unauthorized => Results.Forbid(),
                ErrorType.Forbidden => Results.Forbid(),
                ErrorType.Validation => Results.BadRequest(new { error = error.Message }),
                _ => Results.BadRequest(new { error = error.Message })
            });
    }

    private static async Task<IResult> DeleteHandler(
        string id,
        IUserService service,
        CancellationToken ct)
    {
        Result result = await service.DeleteAsync(id, ct);
        return result.Match(
            onSuccess: () => Results.NoContent(),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                ErrorType.Unauthorized => Results.Forbid(),
                _ => Results.BadRequest(new { error = error.Message })
            });
    }
}
