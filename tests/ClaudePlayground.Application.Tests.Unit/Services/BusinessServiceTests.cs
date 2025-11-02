using System.Linq.Expressions;
using ClaudePlayground.Application.Configuration;
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

public class BusinessServiceTests
{
    // System Under Test
    private readonly BusinessService _sut;

    // Mocks (member variables)
    private readonly IRepository<Business> _businessRepository;
    private readonly IRepository<User> _userRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthService _authService;

    public BusinessServiceTests()
    {
        // Initialize mocks
        _businessRepository = Substitute.For<IRepository<Business>>();
        _userRepository = Substitute.For<IRepository<User>>();
        _tenantProvider = Substitute.For<ITenantProvider>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _authService = Substitute.For<IAuthService>();

        // Create System Under Test
        _sut = new BusinessService(
            _businessRepository,
            _userRepository,
            _tenantProvider,
            _currentUserService,
            _authService
        );
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidIdAndMatchingTenant_ShouldReturnBusiness()
    {
        // Arrange
        var businessId = "business123";
        var tenantId = "tenant123";
        var business = new Business
        {
            Id = businessId,
            TenantId = tenantId,
            Name = "Test Business",
            IsActive = true
        };

        _businessRepository.GetByIdAsync(businessId, Arg.Any<CancellationToken>())
            .Returns(business);

        _tenantProvider.GetTenantId()
            .Returns(tenantId);

        // Act
        var result = await _sut.GetByIdAsync(businessId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(businessId, result.Id);
        Assert.Equal("Test Business", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidIdButDifferentTenant_ShouldReturnNull()
    {
        // Arrange
        var businessId = "business123";
        var business = new Business
        {
            Id = businessId,
            TenantId = "tenant123",
            Name = "Test Business"
        };

        _businessRepository.GetByIdAsync(businessId, Arg.Any<CancellationToken>())
            .Returns(business);

        _tenantProvider.GetTenantId()
            .Returns("differentTenant");

        // Act
        var result = await _sut.GetByIdAsync(businessId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var businessId = "nonexisting123";

        _businessRepository.GetByIdAsync(businessId, Arg.Any<CancellationToken>())
            .Returns((Business?)null);

        // Act
        var result = await _sut.GetByIdAsync(businessId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_AsSuperUser_ShouldReturnAllBusinesses()
    {
        // Arrange
        var businesses = new List<Business>
        {
            new() { Id = "b1", TenantId = "tenant1", Name = "Business 1" },
            new() { Id = "b2", TenantId = "tenant2", Name = "Business 2" },
            new() { Id = "b3", TenantId = "tenant1", Name = "Business 3" }
        };

        _businessRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(businesses);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(true);

        // Act
        var result = await _sut.GetAllAsync(CancellationToken.None);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count);
    }

    [Fact]
    public async Task GetAllAsync_AsNonSuperUser_ShouldReturnOnlyTenantBusinesses()
    {
        // Arrange
        var tenantId = "tenant1";
        var businesses = new List<Business>
        {
            new() { Id = "b1", TenantId = tenantId, Name = "Business 1" },
            new() { Id = "b2", TenantId = "tenant2", Name = "Business 2" },
            new() { Id = "b3", TenantId = tenantId, Name = "Business 3" }
        };

        _businessRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(businesses);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _tenantProvider.GetTenantId()
            .Returns(tenantId);

        // Act
        var result = await _sut.GetAllAsync(CancellationToken.None);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, b => Assert.Equal(tenantId, b.TenantId));
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_ShouldCreateBusiness()
    {
        // Arrange
        var tenantId = "tenant123";
        var addressDto = new AddressDto("123 Main St", "City", "State", "12345", "Country");
        var createDto = new CreateBusinessDto(
            "New Business",
            "Description",
            addressDto,
            "555-1234",
            "test@business.com",
            "www.business.com"
        );

        var createdBusiness = new Business
        {
            Id = "business123",
            TenantId = tenantId,
            Name = "New Business",
            Description = "Description"
        };

        _tenantProvider.GetTenantId()
            .Returns(tenantId);

        _businessRepository.CreateAsync(Arg.Any<Business>(), Arg.Any<CancellationToken>())
            .Returns(createdBusiness);

        // Act
        var result = await _sut.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("business123", result.Id);
        Assert.Equal("New Business", result.Name);
        Assert.Equal(tenantId, result.TenantId);

        await _businessRepository.Received(1).CreateAsync(
            Arg.Is<Business>(b => b.Name == "New Business" && b.TenantId == tenantId),
            Arg.Any<CancellationToken>()
        );
    }

    #endregion

    #region CreateWithUserAsync Tests

    [Fact]
    public async Task CreateWithUserAsync_WithValidDto_ShouldCreateBusinessAndUser()
    {
        // Arrange
        var createDto = new CreateBusinessWithUserDto(
            "New Business",
            "Description",
            null,
            "555-1234",
            "owner@business.com",
            "www.business.com",
            "owner@business.com",
            "password123",
            "John",
            "Doe"
        );

        var createdBusiness = new Business
        {
            Id = "business123",
            TenantId = "business123",
            Name = "New Business"
        };

        var createdUser = new User
        {
            Id = "user123",
            Email = "owner@business.com",
            FirstName = "John",
            LastName = "Doe",
            TenantId = "business123",
            Roles = new List<Role> { Roles.BusinessOwner }
        };

        _userRepository.FindOneAsync(Arg.Any<Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        _businessRepository.CreateAsync(Arg.Any<Business>(), Arg.Any<CancellationToken>())
            .Returns(createdBusiness);

        _userRepository.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(createdUser);

        // Mock AuthService methods
        _authService.GenerateJwtToken(Arg.Any<User>())
            .Returns("jwt-token");

        _authService.GenerateAndSaveRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("refresh-token");

        // Act
        var result = await _sut.CreateWithUserAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Business);
        Assert.Equal("business123", result.Business.Id);
        Assert.Equal("user123", result.UserId);
        Assert.Equal("owner@business.com", result.UserEmail);
        Assert.NotEmpty(result.Token);
        Assert.NotEmpty(result.RefreshToken);

        await _businessRepository.Received(1).CreateAsync(
            Arg.Is<Business>(b => b.TenantId == b.Id),
            Arg.Any<CancellationToken>()
        );

        await _userRepository.Received(1).CreateAsync(
            Arg.Is<User>(u =>
                u.Email == "owner@business.com" &&
                u.Roles.Any(r => r.Value == Roles.BusinessOwnerValue) &&
                u.TenantId == createdBusiness.Id
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task CreateWithUserAsync_WithExistingUserEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateBusinessWithUserDto(
            "New Business",
            "Description",
            null,
            null,
            null,
            null,
            "existing@business.com",
            "password123",
            "John",
            "Doe"
        );

        var existingUser = new User
        {
            Id = "existing123",
            Email = "existing@business.com",
            TenantId = "tenant123"
        };

        _userRepository.FindOneAsync(Arg.Any<Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _sut.CreateWithUserAsync(createDto, CancellationToken.None)
        );

        await _businessRepository.DidNotReceive().CreateAsync(
            Arg.Any<Business>(),
            Arg.Any<CancellationToken>()
        );
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_AsSuperUser_ShouldUpdateAnyBusiness()
    {
        // Arrange
        var businessId = "business123";
        var business = new Business
        {
            Id = businessId,
            TenantId = "tenant1",
            Name = "Old Name"
        };

        var updateDto = new UpdateBusinessDto(
            "New Name",
            "New Description",
            null,
            null,
            null,
            null,
            true
        );

        _businessRepository.GetByIdAsync(businessId, Arg.Any<CancellationToken>())
            .Returns(business);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(true);

        _businessRepository.UpdateAsync(Arg.Any<Business>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Business>());

        // Act
        var result = await _sut.UpdateAsync(businessId, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);

        await _businessRepository.Received(1).UpdateAsync(
            Arg.Is<Business>(b => b.Name == "New Name"),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task UpdateAsync_AsBusinessOwnerWithSameTenant_ShouldUpdateBusiness()
    {
        // Arrange
        var businessId = "business123";
        var tenantId = "tenant123";
        var business = new Business
        {
            Id = businessId,
            TenantId = tenantId,
            Name = "Old Name"
        };

        var updateDto = new UpdateBusinessDto(
            "New Name",
            null,
            null,
            null,
            null,
            null,
            true
        );

        _businessRepository.GetByIdAsync(businessId, Arg.Any<CancellationToken>())
            .Returns(business);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(true);

        _tenantProvider.GetTenantId()
            .Returns(tenantId);

        _businessRepository.UpdateAsync(Arg.Any<Business>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Business>());

        // Act
        var result = await _sut.UpdateAsync(businessId, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_AsBusinessOwnerWithDifferentTenant_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var businessId = "business123";
        var business = new Business
        {
            Id = businessId,
            TenantId = "tenant1",
            Name = "Old Name"
        };

        var updateDto = new UpdateBusinessDto(
            "New Name",
            null,
            null,
            null,
            null,
            null,
            true
        );

        _businessRepository.GetByIdAsync(businessId, Arg.Any<CancellationToken>())
            .Returns(business);

        _currentUserService.IsInRole(Roles.SuperUserValue)
            .Returns(false);

        _currentUserService.IsInRole(Roles.BusinessOwnerValue)
            .Returns(true);

        _tenantProvider.GetTenantId()
            .Returns("differentTenant");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _sut.UpdateAsync(businessId, updateDto, CancellationToken.None)
        );

        await _businessRepository.DidNotReceive().UpdateAsync(
            Arg.Any<Business>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingBusiness_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var businessId = "nonexisting123";
        var updateDto = new UpdateBusinessDto(
            "New Name",
            null,
            null,
            null,
            null,
            null,
            true
        );

        _businessRepository.GetByIdAsync(businessId, Arg.Any<CancellationToken>())
            .Returns((Business?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _sut.UpdateAsync(businessId, updateDto, CancellationToken.None)
        );
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidIdAndMatchingTenant_ShouldDeleteBusiness()
    {
        // Arrange
        var businessId = "business123";
        var tenantId = "tenant123";
        var business = new Business
        {
            Id = businessId,
            TenantId = tenantId,
            Name = "Test Business"
        };

        _businessRepository.GetByIdAsync(businessId, Arg.Any<CancellationToken>())
            .Returns(business);

        _tenantProvider.GetTenantId()
            .Returns(tenantId);

        _businessRepository.DeleteAsync(businessId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.DeleteAsync(businessId, CancellationToken.None);

        // Assert
        Assert.True(result);

        await _businessRepository.Received(1).DeleteAsync(
            businessId,
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task DeleteAsync_WithValidIdButDifferentTenant_ShouldReturnFalse()
    {
        // Arrange
        var businessId = "business123";
        var business = new Business
        {
            Id = businessId,
            TenantId = "tenant123",
            Name = "Test Business"
        };

        _businessRepository.GetByIdAsync(businessId, Arg.Any<CancellationToken>())
            .Returns(business);

        _tenantProvider.GetTenantId()
            .Returns("differentTenant");

        // Act
        var result = await _sut.DeleteAsync(businessId, CancellationToken.None);

        // Assert
        Assert.False(result);

        await _businessRepository.DidNotReceive().DeleteAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Arrange
        var businessId = "nonexisting123";

        _businessRepository.GetByIdAsync(businessId, Arg.Any<CancellationToken>())
            .Returns((Business?)null);

        // Act
        var result = await _sut.DeleteAsync(businessId, CancellationToken.None);

        // Assert
        Assert.False(result);

        await _businessRepository.DidNotReceive().DeleteAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );
    }

    #endregion
}
