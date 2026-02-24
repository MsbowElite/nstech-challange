# Integration Testing Setup - Quick Start Guide

## What Was Created

A complete integration testing framework for the Order Service API with:

### 1. **Infrastructure Files**
- `OrderServiceApiFactory.cs` - Manages the test API host with PostgreSQL Testcontainer
- `IntegrationTestBase.cs` - Base class with common helpers and setup
- `README.md` - Comprehensive documentation

### 2. **Test Files**
- `OrderIntegrationTests.cs` - 10 integration tests for order operations
- `AuthenticationIntegrationTests.cs` - 7 integration tests for auth endpoints

### 3. **Features**
✅ Uses real API with all production dependency injection  
✅ PostgreSQL database in Docker container (Testcontainers)  
✅ Automatic database migration and seeding  
✅ Clean state management between tests  
✅ JWT authentication helpers  
✅ Database access helpers  

## Running Tests

### Prerequisites
1. **Docker Desktop must be running** ⚠️
2. .NET 8.0 SDK installed
3. Visual Studio or VS Code with C# extension

### Run All Integration Tests

```powershell
# Navigate to test project
cd c:\Git\MsbowElite\nstech-challange\src\OrderService.Tests

# Run integration tests only
dotnet test --filter "FullyQualifiedName~Integration"

# Or run all tests
dotnet test
```

### First Run
- Takes longer (~30-60 seconds) - downloading PostgreSQL Docker image
- Subsequent runs are faster (~10-20 seconds total)

### Expected Output
```
Test run for OrderService.Tests.dll (.NETCoreApp,Version=v8.0)
Microsoft (R) Test Execution Command Line Tool Version 17.8.0

Starting test execution, please wait...
A total of 17 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    17, Skipped:     0, Total:    17
```

## Test Coverage

### Order Operations (10 tests)
- ✅ Create order with valid data
- ✅ Create order with insufficient stock
- ✅ Unauthorized access without token
- ✅ Get order by ID (valid and invalid)
- ✅ Get all orders with pagination
- ✅ Cancel order and verify stock restoration
- ✅ Multiple products in single order
- ✅ Validation for empty items
- ✅ Query orders by customer ID

### Authentication (7 tests)
- ✅ User registration
- ✅ Duplicate username handling
- ✅ Login with valid/invalid credentials
- ✅ Protected endpoint access with/without token
- ✅ Invalid token handling

## Architecture

```
IntegrationTestBase (NUnit)
    ↓
OrderServiceApiFactory (WebApplicationFactory)
    ↓
TestContainers.PostgreSql
    ↓
Real OrderService API with production DI
```

## Key Helper Methods

```csharp
// Authentication
await AuthenticateAsync(); // Sets auth token for subsequent requests

// Database Operations
await CreateTestProductAsync("Product", 99.99m, 100);
var product = await GetProductAsync(productId);
await ResetDatabaseAsync(); // Clean state between tests

// Direct DB access
DbContext.Products.Add(product);
await DbContext.SaveChangesAsync();
```

## Troubleshooting

### "Docker is not running"
- Start Docker Desktop
- Verify: `docker ps` in terminal

### Testsdont run
- Check Docker is running
- Ensure NuGet packages restored: `dotnet restore`
- Build solution: `dotnet build`

### Database migration errors
- Ensure migrations exist: `dotnet ef migrations list`
- Apply migrations manually if needed

### Port conflicts
- Testcontainers uses random ports - should not conflict

## Next Steps

### Add More Tests
1. Create new test file in `Integration/` folder
2. Inherit from `IntegrationTestBase`
3. Use `[TestFixture]` and `[Test]` attributes
4. Use provided helper methods

### Example Test Template
```csharp
[TestFixture]
public class MyNewTests : IntegrationTestBase
{
    [Test]
    public async Task MyTest_Scenario_ExpectedResult()
    {
        // Arrange
        await AuthenticateAsync();
        var product = await DbContext.Products.FirstAsync();
        
        // Act
        var response = await Client.GetAsync("/api/endpoint");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## CI/CD Integration

For GitHub Actions or Azure Pipelines, ensure Docker is available:

```yaml
- name: Start Docker
  run: docker version
  
- name: Run Integration Tests
  run: dotnet test --filter "FullyQualifiedName~Integration"
```

## Performance

- Container startup: ~5-10 seconds (one time)
- Average test execution: 2-5 seconds
- Total suite runtime: ~20-30 seconds

## Best Practices

1. **Keep tests independent** - Each test should work in isolation
2. **Use descriptive names** - Follow pattern: `Method_Scenario_ExpectedResult`
3. **Test real scenarios** - Integration tests should test user flows
4. **Use FluentAssertions** - More readable assertions
5. **Clean up between tests** - Use `ResetDatabaseAsync()` when needed

## Files Modified/Created

### Modified
- `OrderService.Tests.csproj` - Added Testcontainers.PostgreSql package
- `Program.cs` - Added `public partial class Program` for testing

### Created
- `Integration/OrderServiceApiFactory.cs`
- `Integration/IntegrationTestBase.cs`
- `Integration/OrderIntegrationTests.cs`
- `Integration/AuthenticationIntegrationTests.cs`
- `Integration/README.md`
- `Integration/QUICKSTART.md` (this file)

---

**🎉 You're all set! Start Docker Desktop and run `dotnet test` to see it in action!**
