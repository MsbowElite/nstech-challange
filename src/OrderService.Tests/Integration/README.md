# Integration Testing with Testcontainers

This directory contains integration tests for the Order Service API that use the real production setup with a PostgreSQL database running in Docker containers via Testcontainers.

## Overview

The integration testing framework provides:

- **Real API Testing**: Uses `WebApplicationFactory` to host the actual API with all production dependency injections
- **Isolated Database**: Each test run uses a fresh PostgreSQL container via Testcontainers
- **Full End-to-End Testing**: Tests HTTP endpoints, authentication, database operations, and business logic
- **Clean State**: Database can be reset between tests for isolation

## Architecture

### Key Components

1. **OrderServiceApiFactory**: Custom `WebApplicationFactory` that:
   - Spins up a PostgreSQL container using Testcontainers
   - Configures the API to use the test database
   - Provides methods to access the database context
   - Handles container lifecycle (start/stop)

2. **IntegrationTestBase**: Base class providing:
   - Common test setup and teardown
   - Authentication helpers (get tokens, set auth headers)
   - Database helpers (seed data, reset state, create test entities)
   - HTTP client configured for the test API

3. **Test Classes**:
   - `OrderIntegrationTests`: Tests for order CRUD operations
   - `AuthenticationIntegrationTests`: Tests for auth endpoints

## Running Tests

### Prerequisites

- Docker Desktop must be running (for Testcontainers)
- .NET 8.0 SDK
- NUnit Test Adapter (installed via NuGet)

### Run All Integration Tests

```powershell
# From the test project directory
dotnet test --filter "FullyQualifiedName~Integration"

# Or run all tests
dotnet test
```

### Run Specific Test Class

```powershell
dotnet test --filter "FullyQualifiedName~OrderIntegrationTests"
```

### Run in Visual Studio

1. Open Test Explorer (Test > Test Explorer)
2. Build the solution
3. Tests will appear under the Integration namespace
4. Click "Run All" or run individual tests

## Writing New Integration Tests

### Basic Test Structure

```csharp
[TestFixture]
public class MyIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task MyTest_Scenario_ExpectedBehavior()
    {
        // Arrange
        await AuthenticateAsync(); // Get auth token if needed
        var productId = await CreateTestProductAsync("Test Product", 100);

        // Act
        var response = await Client.GetAsync($"/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

### Available Helper Methods

From `IntegrationTestBase`:

- `await AuthenticateAsync()` - Sets up authentication for subsequent requests
- `await GetAuthTokenAsync()` - Gets a JWT token
- `await CreateTestProductAsync(name, quantity)` - Creates a product in the database
- `await GetProductAsync(id)` - Retrieves a product from the database
- `await ResetDatabaseAsync()` - Cleans the database and re-seeds test data
- `DbContext` - Direct access to the database context

### Database Access

You have full access to the database context for setup and assertions:

```csharp
// Direct database operations
var product = new Product("Test", 100);
DbContext.Products.Add(product);
await DbContext.SaveChangesAsync();

// Query the database
var orders = await DbContext.Orders
    .Include(o => o.Items)
    .ToListAsync();
```

## Test Isolation

Each test class gets:
- A fresh PostgreSQL container
- Clean database state (via `InitializeAsync`)
- Isolated HTTP client

To reset state between tests in the same class:
```csharp
await ResetDatabaseAsync();
```

## Configuration

The test environment:
- Uses PostgreSQL 16 Alpine (lightweight Docker image)
- Database: `orderservicedb_test`
- Username: `testuser`
- Password: `testpass123`
- Environment: `Testing`

To customize, modify `OrderServiceApiFactory.cs`.

## Features Tested

### Order Endpoints
- ✅ Create order with valid data
- ✅ Create order with insufficient stock
- ✅ Get order by ID
- ✅ Get all orders
- ✅ Update order status
- ✅ Idempotency key handling
- ✅ Multi-product orders
- ✅ Validation errors

### Authentication
- ✅ User registration
- ✅ Duplicate username handling
- ✅ Login with valid credentials
- ✅ Login with invalid credentials
- ✅ Protected endpoint access with token
- ✅ Unauthorized access without token
- ✅ Invalid token handling

## Troubleshooting

### Docker Issues

**Error: "Docker is not running"**
- Ensure Docker Desktop is started
- Check Docker daemon is accessible

**Container startup timeout**
- Increase timeout in `OrderServiceApiFactory`
- Check Docker has sufficient resources

### Test Failures

**Database migration errors**
- Ensure migrations are up to date: `dotnet ef database update`
- Check PostgreSQL container logs

**Authentication failures**
- Verify JWT configuration in test environment
- Check auth endpoints are properly mapped

## Best Practices

1. **Use Fluent Assertions**: Makes tests more readable
   ```csharp
   result.Should().NotBeNull();
   response.StatusCode.Should().Be(HttpStatusCode.OK);
   ```

2. **Test Real Scenarios**: Integration tests should test realistic user flows

3. **Keep Tests Independent**: Each test should work in isolation

4. **Clean Up Resources**: The framework handles cleanup, but avoid leaving orphaned data

5. **Test Negative Cases**: Test error handling, validation, edge cases

6. **Use Descriptive Names**: `CreateOrder_WithInsufficientStock_ShouldReturnBadRequest`

## Performance

- First test run takes longer (container pull and startup)
- Subsequent runs are faster (container image cached)
- Average test execution: 2-5 seconds per test
- Container startup: ~5-10 seconds (one-time per test run)

## CI/CD Integration

For CI environments (GitHub Actions, Azure DevOps):

```yaml
- name: Run Integration Tests
  run: dotnet test --filter "FullyQualifiedName~Integration"
  env:
    DOCKER_HOST: unix:///var/run/docker.sock
```

Ensure the CI runner has Docker available.
