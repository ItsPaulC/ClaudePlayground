using System.Linq.Expressions;
using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Application.Services;
using ClaudePlayground.Domain.Common;
using ClaudePlayground.Domain.Entities;
using ClaudePlayground.Domain.ValueObjects;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace ClaudePlayground.Application.Tests.Unit.Services;

public class UserServiceTests
{
    // System Under Test
    private readonly UserService _sut;

    // Mocks (member variables)
    private readonly IRepository<User> _userRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;

    public UserServiceTests()
    {
        // Initialize mocks
        _userRepository = Substitute.For<IRepository<User>>();
        _tenantProvider = Substitute.For<ITenantProvider>();
        _currentUserService = Substitute.For<ICurrentUserService>();

        // Create System Under Test
        _sut = new UserService(
            _userRepository,
            _tenantProvider,
            _currentUserService
        );
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_AsSuperUser_ShouldReturnAnyUser()
    {
        // Arrange
        string userId = "user123";
        User user = new()
        {
            Id = userId,
            TenantId = "tenant1",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Roles = new List<Role> { Roles.User }
        };

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(true);

        // Act
        Result<UserDto> result = await _sut.GetByIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.Id);
        Assert.Equal("test@example.com", result.Value.Email);
    }

    [Fact]
    public async Task GetByIdAsync_AsBusinessOwnerWithSameTenant_ShouldReturnUser()
    {
        // Arrange
        string userId = "user123";
        string tenantId = "tenant123";
        User user = new()
        {
            Id = userId,
            TenantId = tenantId,
            Email = "test@example.com",
            Roles = new List<Role> { Roles.User }
        };

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(true);

        _tenantProvider.GetTenantId()
            .Returns(tenantId);

        // Act
        Result<UserDto> result = await _sut.GetByIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.Id);
    }

    [Fact]
    public async Task GetByIdAsync_AsBusinessOwnerWithDifferentTenant_ShouldReturnNotFound()
    {
        // Arrange
        string userId = "user123";
        User user = new()
        {
            Id = userId,
            TenantId = "tenant1",
            Email = "test@example.com"
        };

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(true);

        _tenantProvider.GetTenantId()
            .Returns("tenant2");

        // Act
        Result<UserDto> result = await _sut.GetByIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task GetByIdAsync_AsUserViewingThemselves_ShouldReturnUser()
    {
        // Arrange
        string userId = "user123";
        string tenantId = "tenant123";
        User user = new()
        {
            Id = userId,
            TenantId = tenantId,
            Email = "test@example.com"
        };

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(false);

        _tenantProvider.GetTenantId()
            .Returns(tenantId);

        _currentUserService.UserId
            .Returns(userId);

        // Act
        Result<UserDto> result = await _sut.GetByIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.Id);
    }

    [Fact]
    public async Task GetByIdAsync_AsUserViewingOtherUser_ShouldReturnForbidden()
    {
        // Arrange
        string userId = "user123";
        string currentUserId = "different-user";
        string tenantId = "tenant123";
        User user = new()
        {
            Id = userId,
            TenantId = tenantId,
            Email = "test@example.com"
        };

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(false);

        _tenantProvider.GetTenantId()
            .Returns(tenantId);

        _currentUserService.UserId
            .Returns(currentUserId);

        // Act
        Result<UserDto> result = await _sut.GetByIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingUser_ShouldReturnNotFound()
    {
        // Arrange
        string userId = "nonexisting123";

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        Result<UserDto> result = await _sut.GetByIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    #endregion

    #region GetMeAsync Tests

    [Fact]
    public async Task GetMeAsync_WithValidCurrentUser_ShouldReturnCurrentUser()
    {
        // Arrange
        string userId = "user123";
        User user = new()
        {
            Id = userId,
            TenantId = "tenant123",
            Email = "me@example.com",
            FirstName = "Current",
            LastName = "User"
        };

        _currentUserService.UserId
            .Returns(userId);

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        Result<UserDto> result = await _sut.GetMeAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.Id);
        Assert.Equal("me@example.com", result.Value.Email);
    }

    [Fact]
    public async Task GetMeAsync_WithNoCurrentUserId_ShouldReturnUnauthorized()
    {
        // Arrange
        _currentUserService.UserId
            .Returns((string?)null);

        // Act
        Result<UserDto> result = await _sut.GetMeAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);

        await _userRepository.DidNotReceive().GetByIdAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetMeAsync_WithNonExistingCurrentUser_ShouldReturnNotFound()
    {
        // Arrange
        string userId = "user123";

        _currentUserService.UserId
            .Returns(userId);

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        Result<UserDto> result = await _sut.GetMeAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_AsSuperUser_ShouldReturnAllUsers()
    {
        // Arrange
        List<User> users = new()
        {
            new() { Id = "u1", TenantId = "tenant1", Email = "user1@example.com" },
            new() { Id = "u2", TenantId = "tenant2", Email = "user2@example.com" },
            new() { Id = "u3", TenantId = "tenant1", Email = "user3@example.com" }
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(users);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(true);

        // Act
        IEnumerable<UserDto> result = await _sut.GetAllAsync(CancellationToken.None);

        // Assert
        List<UserDto> resultList = result.ToList();
        Assert.Equal(3, resultList.Count);
    }

    [Fact]
    public async Task GetAllAsync_AsBusinessOwner_ShouldReturnOnlyTenantUsers()
    {
        // Arrange
        string tenantId = "tenant1";
        List<User> users = new()
        {
            new() { Id = "u1", TenantId = tenantId, Email = "user1@example.com" },
            new() { Id = "u2", TenantId = "tenant2", Email = "user2@example.com" },
            new() { Id = "u3", TenantId = tenantId, Email = "user3@example.com" }
        };

        _userRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(users);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(true);

        _tenantProvider.GetTenantId()
            .Returns(tenantId);

        // Act
        IEnumerable<UserDto> result = await _sut.GetAllAsync(CancellationToken.None);

        // Assert
        List<UserDto> resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, u => Assert.Equal(tenantId, u.TenantId));
    }

    [Fact]
    public async Task GetAllAsync_AsRegularUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _sut.GetAllAsync(CancellationToken.None)
        );

        await _userRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_AsSuperUserWithValidDto_ShouldCreateUser()
    {
        // Arrange
        CreateUserDto dto = new(
            "newuser@example.com",
            "password123",
            "New",
            "User",
            new[] { Roles.UserValue }
        );

        User createdUser = new()
        {
            Id = "user123",
            Email = "newuser@example.com",
            FirstName = "New",
            LastName = "User",
            TenantId = "tenant123",
            Roles = new List<Role> { Roles.User }
        };

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(true);

        _userRepository.FindOneAsync(Arg.Any<Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        _userRepository.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(createdUser);

        _tenantProvider.GetTenantId()
            .Returns("tenant123");

        // Act
        Result<UserDto> result = await _sut.CreateAsync(dto, null, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("user123", result.Value.Id);
        Assert.Equal("newuser@example.com", result.Value.Email);

        await _userRepository.Received(1).CreateAsync(
            Arg.Is<User>(u => u.Email == "newuser@example.com"),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task CreateAsync_AsBusinessOwnerWithValidRoles_ShouldCreateUser()
    {
        // Arrange
        CreateUserDto dto = new(
            "newuser@example.com",
            "password123",
            "New",
            "User",
            new[] { Roles.UserValue }
        );

        User createdUser = new()
        {
            Id = "user123",
            Email = "newuser@example.com",
            TenantId = "tenant123"
        };

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(true);

        _tenantProvider.GetTenantId()
            .Returns("tenant123");

        _userRepository.FindOneAsync(Arg.Any<Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        _userRepository.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(createdUser);

        // Act
        Result<UserDto> result = await _sut.CreateAsync(dto, null, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_AsBusinessOwnerWithInvalidRole_ShouldReturnUnauthorized()
    {
        // Arrange
        CreateUserDto dto = new(
            "newuser@example.com",
            "password123",
            "New",
            "User",
            new[] { Roles.SuperUserValue }
        );

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(true);

        // Act
        Result<UserDto> result = await _sut.CreateAsync(dto, null, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);

        await _userRepository.DidNotReceive().CreateAsync(
            Arg.Any<User>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task CreateAsync_WithExistingEmail_ShouldReturnConflict()
    {
        // Arrange
        CreateUserDto dto = new(
            "existing@example.com",
            "password123",
            "New",
            "User",
            new[] { Roles.UserValue }
        );

        User existingUser = new()
        {
            Id = "existing123",
            Email = "existing@example.com",
            TenantId = "tenant123"
        };

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(true);

        _userRepository.FindOneAsync(Arg.Any<Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);

        // Act
        Result<UserDto> result = await _sut.CreateAsync(dto, null, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);

        await _userRepository.DidNotReceive().CreateAsync(
            Arg.Any<User>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRoleValue_ShouldReturnValidationError()
    {
        // Arrange
        CreateUserDto dto = new(
            "newuser@example.com",
            "password123",
            "New",
            "User",
            new[] { "invalid-role" }
        );

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(true);

        _userRepository.FindOneAsync(Arg.Any<Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        Result<UserDto> result = await _sut.CreateAsync(dto, null, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_AsSuperUser_ShouldUpdateAnyUser()
    {
        // Arrange
        string userId = "user123";
        User user = new()
        {
            Id = userId,
            TenantId = "tenant1",
            Email = "user@example.com",
            FirstName = "Old",
            LastName = "Name"
        };

        UpdateUserDto dto = new(
            "New",
            "Name",
            true,
            new[] { Roles.UserValue }
        );

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(true);

        _userRepository.UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<User>());

        // Act
        Result<UserDto> result = await _sut.UpdateAsync(userId, dto, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("New", result.Value.FirstName);
        Assert.Equal("Name", result.Value.LastName);

        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.FirstName == "New" && u.LastName == "Name"),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task UpdateAsync_AsBusinessOwnerWithSameTenant_ShouldUpdateUser()
    {
        // Arrange
        string userId = "user123";
        string tenantId = "tenant123";
        User user = new()
        {
            Id = userId,
            TenantId = tenantId,
            Email = "user@example.com"
        };

        UpdateUserDto dto = new(
            "Updated",
            "Name",
            true,
            new[] { Roles.UserValue }
        );

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(true);

        _tenantProvider.GetTenantId()
            .Returns(tenantId);

        _userRepository.UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<User>());

        // Act
        Result<UserDto> result = await _sut.UpdateAsync(userId, dto, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Updated", result.Value.FirstName);
    }

    [Fact]
    public async Task UpdateAsync_AsBusinessOwnerWithDifferentTenant_ShouldReturnNotFound()
    {
        // Arrange
        string userId = "user123";
        User user = new()
        {
            Id = userId,
            TenantId = "tenant1",
            Email = "user@example.com"
        };

        UpdateUserDto dto = new(
            "Updated",
            "Name",
            true,
            new[] { Roles.UserValue }
        );

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(true);

        _tenantProvider.GetTenantId()
            .Returns("tenant2");

        // Act
        Result<UserDto> result = await _sut.UpdateAsync(userId, dto, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);

        await _userRepository.DidNotReceive().UpdateAsync(
            Arg.Any<User>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task UpdateAsync_AsBusinessOwnerWithInvalidRole_ShouldReturnForbidden()
    {
        // Arrange
        string userId = "user123";
        string tenantId = "tenant123";
        User user = new()
        {
            Id = userId,
            TenantId = tenantId,
            Email = "user@example.com"
        };

        UpdateUserDto dto = new(
            "Updated",
            "Name",
            true,
            new[] { Roles.SuperUserValue }
        );

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(true);

        _tenantProvider.GetTenantId()
            .Returns(tenantId);

        // Act
        Result<UserDto> result = await _sut.UpdateAsync(userId, dto, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
    }

    [Fact]
    public async Task UpdateAsync_AsRegularUser_ShouldReturnUnauthorized()
    {
        // Arrange
        string userId = "user123";
        UpdateUserDto dto = new(
            "Updated",
            "Name",
            true,
            new[] { Roles.UserValue }
        );

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(false);

        // Act
        Result<UserDto> result = await _sut.UpdateAsync(userId, dto, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);

        await _userRepository.DidNotReceive().GetByIdAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingUser_ShouldReturnNotFound()
    {
        // Arrange
        string userId = "nonexisting123";
        UpdateUserDto dto = new(
            "Updated",
            "Name",
            true,
            new[] { Roles.UserValue }
        );

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(true);

        // Act
        Result<UserDto> result = await _sut.UpdateAsync(userId, dto, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_AsSuperUser_ShouldDeleteAnyUser()
    {
        // Arrange
        string userId = "user123";
        User user = new()
        {
            Id = userId,
            TenantId = "tenant1",
            Email = "user@example.com"
        };

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(true);

        _userRepository.DeleteAsync(userId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        Result result = await _sut.DeleteAsync(userId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        await _userRepository.Received(1).DeleteAsync(
            userId,
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task DeleteAsync_AsBusinessOwnerWithSameTenant_ShouldDeleteUser()
    {
        // Arrange
        string userId = "user123";
        string tenantId = "tenant123";
        User user = new()
        {
            Id = userId,
            TenantId = tenantId,
            Email = "user@example.com"
        };

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(true);

        _tenantProvider.GetTenantId()
            .Returns(tenantId);

        _userRepository.DeleteAsync(userId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        Result result = await _sut.DeleteAsync(userId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_AsBusinessOwnerWithDifferentTenant_ShouldReturnNotFound()
    {
        // Arrange
        string userId = "user123";
        User user = new()
        {
            Id = userId,
            TenantId = "tenant1",
            Email = "user@example.com"
        };

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(true);

        _tenantProvider.GetTenantId()
            .Returns("tenant2");

        // Act
        Result result = await _sut.DeleteAsync(userId, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);

        await _userRepository.DidNotReceive().DeleteAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task DeleteAsync_AsRegularUser_ShouldReturnUnauthorized()
    {
        // Arrange
        string userId = "user123";

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(false);

        // Act
        Result result = await _sut.DeleteAsync(userId, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);

        await _userRepository.DidNotReceive().GetByIdAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingUser_ShouldReturnNotFound()
    {
        // Arrange
        string userId = "nonexisting123";

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(true);

        // Act
        Result result = await _sut.DeleteAsync(userId, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);

        await _userRepository.DidNotReceive().DeleteAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );
    }

    #endregion

    #region ValidateUserCreationAuthorization Tests (Internal Method)

    [Fact]
    public void ValidateUserCreationAuthorization_AsSuperUserWithAnyRole_ShouldNotThrow()
    {
        // Arrange
        IEnumerable<string> roleValues = new[] { Roles.SuperUserValue, Roles.BusinessOwnerValue };

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(true);

        // Act & Assert - Should not throw
        _sut.ValidateUserCreationAuthorization(roleValues, null);
    }

    [Fact]
    public void ValidateUserCreationAuthorization_AsBusinessOwnerWithValidRoles_ShouldNotThrow()
    {
        // Arrange
        IEnumerable<string> roleValues = new[] { Roles.UserValue, Roles.ReadOnlyUserValue };

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(true);

        // Act & Assert - Should not throw
        _sut.ValidateUserCreationAuthorization(roleValues, null);
    }

    [Fact]
    public void ValidateUserCreationAuthorization_AsBusinessOwnerWithInvalidRole_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        IEnumerable<string> roleValues = new[] { Roles.SuperUserValue };

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(true);

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(
            () => _sut.ValidateUserCreationAuthorization(roleValues, null)
        );
    }

    [Fact]
    public void ValidateUserCreationAuthorization_AsBusinessOwnerWithDifferentTargetTenant_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        IEnumerable<string> roleValues = new[] { Roles.UserValue };
        string targetTenantId = "tenant2";

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(true);

        _tenantProvider.GetTenantId()
            .Returns("tenant1");

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(
            () => _sut.ValidateUserCreationAuthorization(roleValues, targetTenantId)
        );
    }

    [Fact]
    public void ValidateUserCreationAuthorization_AsRegularUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        IEnumerable<string> roleValues = new[] { Roles.UserValue };

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(false);

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(
            () => _sut.ValidateUserCreationAuthorization(roleValues, null)
        );
    }

    #endregion
}
