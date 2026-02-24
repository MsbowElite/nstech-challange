# Integration Testing with Testcontainers - Summary

## Overview

A complete integration testing framework has been created for the Order Service API. The tests use:
- **WebApplicationFactory** to host the real API with production dependency injection
- **Testcontainers** to run PostgreSQL in Docker for isolated, reproducible tests
- **NUnit** as the testing framework
- **FluentAssertions** for readable test assertions

## What Was Created

### Core Infrastructure (in `src/OrderService.Tests/Integration/`)

1. **OrderServiceApiFactory.cs**
   - Custom `WebApplicationFactory<Program>` 
   - Manages PostgreSQL Testcontainer lifecycle
   - Replaces production database with test database
   - Provides database access methods for tests

2. **IntegrationTestBase.cs**
   - Base class for all integration tests
   - NUnit setup/teardown using `[OneTimeSetUp]` and `[OneTimeTearDown]`
   - Helper methods for authentication, database operations, and HTTP requests
   - Automatic test data seeding

3. **OrderIntegrationTests.cs**
   - 10 comprehensive tests for order operations
   - Tests CRUD operations, validation, authorization, stock management

4. **AuthenticationIntegrationTests.cs**
   - 7 tests for authentication endpoints
   - Tests registration, login, token validation, protected endpoint access

### Documentation

- `Integration/README.md` - Comprehensive guide to the framework
- `Integration/QUICKSTART.md` - Quick start guide for running tests

### Dependencies Added

```xml
<PackageReference Include="Testcontainers.PostgreSql" Version="3.10.0" />
```

## Running the Tests

### Prerequisites
⚠️ **Docker Desktop must be running**

### Execute Tests

```powershell
# Navigate to test project
cd src/OrderService.Tests

# Run only integration tests
dotnet test --filter "FullyQualifiedName~Integration"

# Run all tests
dotnet test
```

### First Run
- Downloads PostgreSQL Docker image (~30-60 seconds)
- Subsequent runs are much faster (~10-20 seconds)

## Test Coverage

### Order Operations ✅
- Create order with valid data & verify stock reduction
- Create order with insufficient stock → BadRequest
- Unauthorized access without JWT token
- Get order by ID (valid & invalid cases)
- Get paginated list of orders
- Cancel order & verify stock restoration
- Multiple products in single order
- Query orders by customer ID
- Validation for empty items

### Authentication ✅
- User registration & token generation
- Duplicate username prevention
- Login with valid/invalid credentials
- Protected endpoint access with valid token
- Unauthorized access without token
- Invalid token rejection

## Key Features

### 1. Production-Like Environment
- Uses the real API (`Program.cs`) with all production services
- Real database (PostgreSQL in Docker)
- Real authentication (JWT)
- Real validation and business logic

### 2. Isolated & Repeatable
- Each test run gets a fresh PostgreSQL container
- Database is automatically migrated and seeded
- Tests can reset database state when needed
- No test pollution between runs

### 3. Developer-Friendly Helpers

```csharp
// Automatic authentication
await AuthenticateAsync();

// Database operations
var productId = await CreateTestProductAsync("Laptop", 999.99m, 50);
var product = await GetProductAsync(productId);

// Direct DB access when needed
DbContext.Products.Add(newProduct);
await DbContext.SaveChangesAsync();

// Clean state
await ResetDatabaseAsync();
```

### 4. Comprehensive Testing
- HTTP requests/responses
- Database state verification
- Authentication & authorization
- Business logic validation
- Error handling

## Architecture

```
┌─────────────────────────────────┐
│  IntegrationTestBase (NUnit)    │
│  - OneTimeSetUp/TearDown        │
│  - Authentication helpers       │
│  - Database helpers             │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│  OrderServiceApiFactory         │
│  (WebApplicationFactory)        │
│  - Configures test services     │
│  - Manages container lifecycle  │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│  PostgreSQL Testcontainer       │
│  - Fresh database per test run  │
│  - Auto-migrated & seeded       │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│  Real OrderService API          │
│  - All production DI            │
│  - Real endpoints               │
│  - Real business logic          │
└─────────────────────────────────┘
```

## Example Test

```csharp
[Test]
public async Task CreateOrder_WithValidData_ShouldCreateOrderSuccessfully()
{
    // Arrange
    await AuthenticateAsync(); // Helper method sets JWT token
    var product = await DbContext.Products.FirstAsync();
    var initialStock = product.AvailableQuantity;

    var createOrderRequest = new CreateOrderRequest(
        CustomerId: Guid.NewGuid(),
        Currency: "USD",
        Items: new List<OrderItemRequest>
        {
            new OrderItemRequest(product.Id, 2)
        }
    );

    // Act - Real HTTP call to API
    var response = await Client.PostAsJsonAsync("/api/orders", createOrderRequest);

    // Assert - Verify HTTP response
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    
    var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
    order.Should().NotBeNull();
    order!.Items.Should().HaveCount(1);
    
    // Verify database state
    var updatedProduct = await GetProductAsync(product.Id);
    updatedProduct!.AvailableQuantity.Should().Be(initialStock - 2);
}
```

## Benefits

### ✅ Confidence
- Tests the real application end-to-end
- Catches integration issues that unit tests miss
- Validates database migrations and queries

### ✅ Maintainability
- Uses production code - no mocking of major components
- Changes to business logic automatically flow to tests
- Clear, readable test code with helpers

### ✅ Speed
- Testcontainers handles container lifecycle automatically
- Parallel test execution possible (each class gets own container)
- Faster than manual database setup

### ✅ CI/CD Ready
- Works in any environment with Docker
- No manual database setup required
- Reproducible across machines

## Next Steps

### Add More Tests
Create new test files in `src/OrderService.Tests/Integration/`:

```csharp
[TestFixture]
public class ProductIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetProducts_ShouldReturnAllProducts()
    {
        // Your test here
    }
}
```

### Customize Test Data
Override `SeedTestDataAsync()` in your test class:

```csharp
protected override async Task SeedTestDataAsync()
{
    await base.SeedTestDataAsync();
    // Add your custom test data
}
```

### Add Integration to CI/CD
Example GitHub Actions:

```yaml
- name: Run Integration Tests
  run: dotnet test --filter "FullyQualifiedName~Integration"
  env:
    DOCKER_HOST: unix:///var/run/docker.sock
```

## Files Changed

### Modified Files
1. `src/OrderService.API/Program.cs`
   - Added: `public partial class Program { }` to make it accessible from tests

2. `src/OrderService.Tests/OrderService.Tests.csproj`
   - Added: `Testcontainers.PostgreSql` NuGet package

### New Files Created
1. `src/OrderService.Tests/Integration/OrderServiceApiFactory.cs`
2. `src/OrderService.Tests/Integration/IntegrationTestBase.cs`
3. `src/OrderService.Tests/Integration/OrderIntegrationTests.cs`
4. `src/OrderService.Tests/Integration/AuthenticationIntegrationTests.cs`
5. `src/OrderService.Tests/Integration/README.md`
6. `src/OrderService.Tests/Integration/QUICKSTART.md`
7. `docs/INTEGRATION-TESTING-SUMMARY.md` (this file)

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Docker is not running" | Start Docker Desktop and ensure it's running |
| Tests timeout | Increase Docker resources (Memory/CPU) |
| Container pull fails | Check internet connection, verify Docker Hub access |
| Build errors | Run `dotnet restore` and `dotnet build` |
| Database migrations fail | Ensure migrations are up to date in the project |

## Resources

- [Testcontainers Documentation](https://dotnet.testcontainers.org/)
- [WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [NUnit Documentation](https://docs.nunit.org/)
- [FluentAssertions](https://fluentassertions.com/)

---

**Ready to test!** 🚀 Start Docker Desktop and run `dotnet test --filter "FullyQualifiedName~Integration"`
