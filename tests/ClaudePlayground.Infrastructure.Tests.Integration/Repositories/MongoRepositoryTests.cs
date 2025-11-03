using ClaudePlayground.Domain.Entities;
using ClaudePlayground.Domain.ValueObjects;
using ClaudePlayground.Infrastructure.Repositories;
using ClaudePlayground.Infrastructure.Tests.Integration.Fixtures;

namespace ClaudePlayground.Infrastructure.Tests.Integration.Repositories;

public class MongoRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoRepository<Business> _repository;

    public MongoRepositoryTests(MongoDbFixture fixture)
    {
        _repository = new MongoRepository<Business>(fixture.MongoDbContext);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateBusinessEntity()
    {
        // Arrange
        Business business = new()
        {
            TenantId = "tenant-123",
            Name = "Test Business",
            Description = "Test Description",
            Address = new Address("123 Main St", "Springfield", "IL", "62701", "USA"),
            PhoneNumber = "555-1234",
            Email = "test@business.com",
            Website = "https://testbusiness.com",
            IsActive = true
        };

        // Act
        Business created = await _repository.CreateAsync(business, CancellationToken.None);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(business.Id, created.Id);
        Assert.Equal("tenant-123", created.TenantId);
        Assert.Equal("Test Business", created.Name);
        Assert.NotEqual(default, created.CreatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnBusinessEntity_WhenExists()
    {
        // Arrange
        Business business = new()
        {
            TenantId = "tenant-456",
            Name = "Get Test Business",
            Email = "get@test.com"
        };
        Business created = await _repository.CreateAsync(business, CancellationToken.None);

        // Act
        Business? result = await _repository.GetByIdAsync(created.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("tenant-456", result.TenantId);
        Assert.Equal("Get Test Business", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        string nonExistentId = Guid.NewGuid().ToString();

        // Act
        Business? result = await _repository.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllBusinessEntities()
    {
        // Arrange
        Business business1 = new()
        {
            TenantId = "tenant-789",
            Name = "Business 1",
            Email = "business1@test.com"
        };
        Business business2 = new()
        {
            TenantId = "tenant-789",
            Name = "Business 2",
            Email = "business2@test.com"
        };
        await _repository.CreateAsync(business1, CancellationToken.None);
        await _repository.CreateAsync(business2, CancellationToken.None);

        // Act
        IEnumerable<Business> results = await _repository.GetAllAsync(cancellationToken: CancellationToken.None);

        // Assert
        Assert.NotNull(results);
        IEnumerable<Business> businesses = results as Business[] ?? results.ToArray();
        Assert.True(businesses.Count() >= 2);
        Assert.Contains(businesses, b => b.Name == "Business 1");
        Assert.Contains(businesses, b => b.Name == "Business 2");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateBusinessEntity()
    {
        // Arrange
        Business business = new()
        {
            TenantId = "tenant-update",
            Name = "Original Name",
            Email = "original@test.com"
        };
        Business created = await _repository.CreateAsync(business, CancellationToken.None);

        // Act
        created.Name = "Updated Name";
        created.Email = "updated@test.com";
        Business updated = await _repository.UpdateAsync(created, CancellationToken.None);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("updated@test.com", updated.Email);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAddress()
    {
        // Arrange
        Business business = new()
        {
            TenantId = "tenant-address",
            Name = "Address Test Business",
            Address = new Address("123 Old St", "OldCity", "OldState", "12345", "USA")
        };
        Business created = await _repository.CreateAsync(business, CancellationToken.None);

        // Act
        created.Address = new Address("456 New Ave", "NewCity", "NewState", "67890", "USA");
        Business updated = await _repository.UpdateAsync(created, CancellationToken.None);

        // Assert
        Assert.NotNull(updated);
        Assert.NotNull(updated.Address);
        Assert.Equal("456 New Ave", updated.Address.Street);
        Assert.Equal("NewCity", updated.Address.City);
        Assert.Equal("NewState", updated.Address.State);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteBusinessEntity_WhenExists()
    {
        // Arrange
        Business business = new()
        {
            TenantId = "tenant-delete",
            Name = "Delete Test Business",
            Email = "delete@test.com"
        };
        Business created = await _repository.CreateAsync(business, CancellationToken.None);

        // Act
        bool deleted = await _repository.DeleteAsync(created.Id, CancellationToken.None);

        // Assert
        Assert.True(deleted);

        // Verify deletion
        Business? result = await _repository.GetByIdAsync(created.Id, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Arrange
        string nonExistentId = Guid.NewGuid().ToString();

        // Act
        bool deleted = await _repository.DeleteAsync(nonExistentId, CancellationToken.None);

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public async Task CreateAsync_ShouldSetCreatedAtToUtc()
    {
        // Arrange
        Business business = new()
        {
            TenantId = "tenant-time",
            Name = "Time Test Business"
        };

        // Act
        Business created = await _repository.CreateAsync(business, CancellationToken.None);

        // Assert
        Assert.Equal(DateTimeKind.Utc, created.CreatedAt.Kind);
        Assert.True(created.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task UpdateAsync_ShouldSetUpdatedAtToUtc()
    {
        // Arrange
        Business business = new()
        {
            TenantId = "tenant-updatetime",
            Name = "Update Time Test"
        };
        Business created = await _repository.CreateAsync(business, CancellationToken.None);

        // Act
        created.Name = "Modified Name";
        Business updated = await _repository.UpdateAsync(created, CancellationToken.None);

        // Assert
        Assert.NotNull(updated.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, updated.UpdatedAt.Value.Kind);
        Assert.True(updated.UpdatedAt <= DateTime.UtcNow);
    }
}
