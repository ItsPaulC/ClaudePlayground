# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This is a .NET 8 Web API project implementing **Clean Architecture** principles with MongoDB as the database. The solution is structured in four layers with clear separation of concerns.

## Project Structure

```
ClaudePlayground/
├── ClaudePlayground.slnx              # Solution file (XML-based .slnx format)
├── docker-compose.yml                 # MongoDB container configuration
├── CLAUDE.md                          # Documentation for Claude Code
├── src/                               # Source code directory
│   ├── ClaudePlayground.Api/          # Presentation Layer (Web API)
│   │   ├── Program.cs                 # Entry point and DI configuration
│   │   ├── appsettings.json           # Configuration (MongoDB connection, etc.)
│   │   ├── Endpoints/                 # Endpoint definitions organized by feature
│   │   │   └── BusinessEndpoints.cs
│   │   └── *.http                     # HTTP request files for testing
│   ├── ClaudePlayground.Application/  # Application Layer (Use Cases)
│   │   ├── DTOs/                      # Data Transfer Objects
│   │   ├── Interfaces/                # Service interfaces
│   │   └── Services/                  # Service implementations
│   ├── ClaudePlayground.Domain/       # Domain Layer (Core Business Logic)
│   │   ├── Entities/                  # Domain entities (Business, BaseEntity)
│   │   ├── ValueObjects/              # Value objects (Address)
│   │   └── Common/                    # Repository interfaces and shared contracts
│   └── ClaudePlayground.Infrastructure/ # Infrastructure Layer (External Concerns)
│       ├── Configuration/             # Settings and configuration models
│       ├── Persistence/               # Database context
│       ├── Repositories/              # Repository implementations
│       ├── Tenancy/                   # Multi-tenancy providers
│       └── DependencyInjection.cs     # Infrastructure service registration
└── tests/                             # Test projects directory
    └── ClaudePlayground.Infrastructure.Tests.Integration/
        ├── Fixtures/                  # Test fixtures (e.g., MongoDbFixture)
        └── Repositories/              # Repository integration tests
```

## Common Commands

### Solution-Level Commands
Run these from the `ClaudePlayground` directory:

```bash
# Build the entire solution
dotnet build ClaudePlayground.slnx

# Restore all projects
dotnet restore ClaudePlayground.slnx

# Clean all projects
dotnet clean ClaudePlayground.slnx

# Run all tests
dotnet test ClaudePlayground.slnx
```

### Project-Level Commands
Run these from `ClaudePlayground/src/ClaudePlayground.Api`:

```bash
# Build the project
dotnet build

# Run the application (starts on https://localhost:5001 and http://localhost:5000)
dotnet run

# Run with hot reload (watches for file changes)
dotnet watch run

# Clean build artifacts
dotnet clean

# Format code
dotnet format
```

### Package Management
```bash
# Add a NuGet package (from project directory)
dotnet add package <PackageName>

# Restore dependencies
dotnet restore

# List installed packages
dotnet list package
```

### Testing
```bash
# Run all tests in the solution
dotnet test ClaudePlayground.slnx

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests with coverage
dotnet test /p:CollectCoverage=true

# Run tests from specific test project
dotnet test tests/ClaudePlayground.Infrastructure.Tests.Integration
```

**Prerequisites for Integration Tests:**
- Docker Desktop must be running (Testcontainers requires Docker)
- The integration tests automatically spin up a MongoDB container via Testcontainers
- No manual setup required - the test infrastructure handles container lifecycle

**Test Framework Stack:**
- **xUnit v3 (3.1.0)**: Testing framework
- **NSubstitute 5.3.0**: Mocking framework (when needed)
- **Testcontainers.MongoDb 4.1.0**: Provides real MongoDB instances for integration tests

### MongoDB Commands
```bash
# Start MongoDB container (from ClaudePlayground directory)
docker-compose up -d

# Stop MongoDB container
docker-compose down

# View MongoDB logs
docker-compose logs mongodb

# Connect to MongoDB shell
docker exec -it claudeplayground-mongodb mongosh
```

### Solution Management
The project uses the modern `.slnx` format (XML-based solution file):

```bash
# Add a new project to solution (from ClaudePlayground directory)
dotnet sln ClaudePlayground.slnx add <path-to-project.csproj>

# Remove a project from solution
dotnet sln ClaudePlayground.slnx remove <path-to-project.csproj>

# List projects in solution
dotnet sln ClaudePlayground.slnx list
```

## Architecture

This project follows **Clean Architecture** (also known as Onion Architecture or Hexagonal Architecture) principles. The architecture enforces separation of concerns and dependency inversion.

### Layer Dependencies

```
┌─────────────────────────────────────┐
│    Presentation (API)               │
│    - Endpoints                      │
│    - HTTP concerns                  │
└──────────────┬──────────────────────┘
               │ depends on
┌──────────────▼──────────────────────┐
│    Infrastructure                   │
│    - MongoDB repositories           │
│    - External services              │
└──────────────┬──────────────────────┘
               │ depends on
┌──────────────▼──────────────────────┐
│    Application                      │
│    - DTOs                           │
│    - Service interfaces & impls     │
└──────────────┬──────────────────────┘
               │ depends on
┌──────────────▼──────────────────────┐
│    Domain (Core)                    │
│    - Entities                       │
│    - Repository interfaces          │
│    - Business rules                 │
└─────────────────────────────────────┘
```

### Layer Descriptions

#### 1. Domain Layer (`ClaudePlayground.Domain`)
- **Purpose**: Contains core business logic and domain entities
- **Dependencies**: None (no dependencies on other projects)
- **Key Concepts**:
  - `BaseEntity`: Base class for all entities with Id, CreatedAt, UpdatedAt
  - `IRepository<T>`: Generic repository interface defining data access contracts
  - Domain entities (e.g., `WeatherForecast`)

#### 2. Application Layer (`ClaudePlayground.Application`)
- **Purpose**: Contains application business logic and use cases
- **Dependencies**: Domain layer only
- **Key Concepts**:
  - **DTOs**: Data Transfer Objects for external communication
  - **Interfaces**: Service contracts (e.g., `IWeatherForecastService`)
  - **Services**: Implementation of business logic using repository interfaces
  - Maps between domain entities and DTOs

#### 3. Infrastructure Layer (`ClaudePlayground.Infrastructure`)
- **Purpose**: Implements interfaces from Domain/Application for external concerns
- **Dependencies**: Domain and Application layers
- **Key Concepts**:
  - **MongoDB**: Database implementation using MongoDB.Driver
  - **Repositories**: Concrete implementations of `IRepository<T>`
  - **MongoDbContext**: Database context for MongoDB collections
  - **DependencyInjection**: Extension method to register all infrastructure services

#### 4. Presentation Layer (`ClaudePlayground.Api`)
- **Purpose**: HTTP API endpoints and presentation concerns
- **Dependencies**: Application and Infrastructure layers
- **Key Concepts**:
  - **Endpoints**: Organized by feature in separate files using extension methods
  - **MapGroup**: Groups related endpoints with common prefixes and tags
  - Minimal API endpoints using `app.MapGet()`, `MapPost()`, etc.
  - OpenAPI/Swagger documentation
  - Dependency injection configuration in Program.cs

### Database Configuration

The application uses **MongoDB** as the database. Configuration is in `appsettings.json`:

```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "ClaudePlayground"
  }
}
```

Collections are automatically created based on entity types with pluralized names (e.g., `weatherforecasts` for `WeatherForecast`).

### Adding New Features

#### 1. Add a New Entity
Create entity in `Domain/Entities/`:
```csharp
public class YourEntity : BaseEntity
{
    public string Property { get; set; }
}
```

#### 2. Create DTOs
Create DTOs in `Application/DTOs/`:
```csharp
public record YourEntityDto(string Id, string Property, DateTime CreatedAt);
public record CreateYourEntityDto(string Property);
public record UpdateYourEntityDto(string Property);
```

#### 3. Create Service Interface and Implementation
Create interface in `Application/Interfaces/`:
```csharp
public interface IYourEntityService
{
    Task<YourEntityDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<YourEntityDto>> GetAllAsync(CancellationToken ct = default);
    // ... other methods
}
```

Create implementation in `Application/Services/`:
```csharp
public class YourEntityService : IYourEntityService
{
    private readonly IRepository<YourEntity> _repository;

    public YourEntityService(IRepository<YourEntity> repository)
    {
        _repository = repository;
    }

    // Implement methods
}
```

#### 4. Register Service
Add to `Infrastructure/DependencyInjection.cs`:
```csharp
services.AddScoped<IYourEntityService, YourEntityService>();
```

#### 5. Add API Endpoints
Create endpoints file in `Api/Endpoints/YourEntityEndpoints.cs`:
```csharp
using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;

namespace ClaudePlayground.Api.Endpoints;

public static class YourEntityEndpoints
{
    public static IEndpointRouteBuilder MapYourEntityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/yourentities")
            .WithTags("Your Entities");

        group.MapGet("/", async (IYourEntityService service, CancellationToken ct) =>
        {
            var entities = await service.GetAllAsync(ct);
            return Results.Ok(entities);
        })
        .WithName("GetAllYourEntities")
        .WithOpenApi();

        // Add other endpoints (POST, PUT, DELETE) here

        return app;
    }
}
```

Then register in `Api/Program.cs`:
```csharp
app.MapYourEntityEndpoints();
```

### Design Principles

- **Dependency Inversion**: Core layers define interfaces; outer layers implement them
- **Single Responsibility**: Each layer has one reason to change
- **Testability**: Core business logic is isolated and easy to unit test
- **Database Agnostic Core**: Domain and Application layers have no knowledge of MongoDB
- **Repository Pattern**: Abstraction over data persistence

## Testing Strategy

### Integration Tests

The solution includes integration tests for the Infrastructure layer that test actual MongoDB operations against real database instances using Testcontainers.

#### Test Project: `ClaudePlayground.Infrastructure.Tests.Integration`

**Location**: `tests/ClaudePlayground.Infrastructure.Tests.Integration/`

**Purpose**: Verify that repository implementations correctly interact with MongoDB using real database instances.

#### Test Fixtures

**MongoDbFixture** (`Fixtures/MongoDbFixture.cs`):
- Implements `IAsyncLifetime` for test lifecycle management
- Automatically starts a MongoDB container before tests run
- Provides a configured `MongoDbContext` to tests
- Automatically cleans up and stops the container after tests complete
- Shared across all tests in a test class using `IClassFixture<MongoDbFixture>`
- **xUnit v3 Note**: `IAsyncLifetime` methods return `ValueTask` instead of `Task` in v3

```csharp
public class MongoRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;
    private readonly MongoRepository<Business> _repository;

    public MongoRepositoryTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _repository = new MongoRepository<Business>(_fixture.MongoDbContext);
    }

    // Test methods...
}
```

#### Test Coverage

The integration tests cover:
- **CreateAsync**: Creating entities, verifying field mapping, UTC timestamp handling
- **GetByIdAsync**: Retrieving existing entities and handling non-existent IDs
- **GetAllAsync**: Querying multiple entities
- **UpdateAsync**: Updating entity properties and nested value objects (Address)
- **DeleteAsync**: Deleting entities and verifying deletion

#### Running Integration Tests

Integration tests require Docker Desktop to be running:

```bash
# Ensure Docker Desktop is running first

# Run all integration tests
dotnet test tests/ClaudePlayground.Infrastructure.Tests.Integration

# Or run from solution root
dotnet test ClaudePlayground.slnx
```

**Test Execution Flow:**
1. xUnit discovers test classes with `IClassFixture<MongoDbFixture>`
2. MongoDbFixture.InitializeAsync() starts a MongoDB container
3. Tests run against the real MongoDB instance
4. MongoDbFixture.DisposeAsync() stops and removes the container

#### Benefits of Testcontainers

- **Real Database Testing**: Tests run against actual MongoDB, not mocks
- **Isolation**: Each test class gets a fresh database instance
- **CI/CD Ready**: No manual infrastructure setup required
- **Reliability**: Catches integration issues that unit tests miss
- **Consistency**: Same MongoDB version across all environments

### Adding New Integration Tests

1. Create a test class that uses `IClassFixture<MongoDbFixture>`:
```csharp
public class YourRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;

    public YourRepositoryTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }
}
```

2. If creating a custom fixture, implement `IAsyncLifetime` with `ValueTask` (xUnit v3):
```csharp
public class YourFixture : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        // Setup code
    }

    public async ValueTask DisposeAsync()
    {
        // Cleanup code
    }
}
```

3. Write test methods using `[Fact]` attribute and inject dependencies:
```csharp
[Fact]
public async Task YourTest()
{
    // Arrange
    MongoRepository<YourEntity> repository =
        new MongoRepository<YourEntity>(_fixture.MongoDbContext);

    // Act & Assert
}
```

4. Follow AAA (Arrange-Act-Assert) pattern and use xUnit assertions
