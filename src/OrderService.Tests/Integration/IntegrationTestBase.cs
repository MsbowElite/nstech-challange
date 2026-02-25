using NUnit.Framework;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace OrderService.Tests.Integration;

/// <summary>
/// Base class for integration tests with common utilities and setup
/// </summary>
public abstract class IntegrationTestBase
{
    protected OrderServiceApiFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;
    protected HttpClient ClientNoAuth { get; private set; } = null!;
    protected OrderDbContext DbContext { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        Factory = new OrderServiceApiFactory();
        await Factory.StartAsync();

        Client = Factory.CreateClient();
        ClientNoAuth = Factory.CreateClient();
        DbContext = Factory.GetDbContext();

        // Seed initial data if needed
        await SeedTestDataAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        Client?.Dispose();
        DbContext?.Dispose();
        if (Factory != null)
        {
            await Factory.StopAsync();
        }
    }

    /// <summary>
    /// Override this method to seed test data before each test
    /// </summary>
    protected virtual async Task SeedTestDataAsync()
    {
        // Default: Seed some products for testing
        if (!DbContext.Products.Any())
        {
            var products = new List<Product>
            {
                new Product("Laptop", 999.99m, 10),
                new Product("Mouse", 29.99m, 50),
                new Product("Keyboard", 79.99m, 30),
                new Product("Monitor", 299.99m, 15)
            };

            DbContext.Products.AddRange(products);
            await DbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Gets a valid JWT token for authentication
    /// </summary>
    protected async Task<string> GetAuthTokenAsync(string username = "testuser", string password = "testpass")
    {
        var tokenRequest = new
        {
            username = username,
            password = password
        };

        var response = await Client.PostAsJsonAsync("/auth/token", tokenRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return result?.AccessToken ?? throw new InvalidOperationException("Failed to get auth token");
    }

    /// <summary>
    /// Sets the authorization header with a valid JWT token
    /// </summary>
    protected async Task AuthenticateAsync(string username = "testuser", string password = "testpass")
    {
        var token = await GetAuthTokenAsync(username, password);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Gets a product from the database
    /// </summary>
    protected async Task<Product?> GetProductAsync(Guid productId)
    {
        DbContext.ChangeTracker.Clear();
        return await DbContext.Products.FindAsync(productId);
    }

    /// <summary>
    /// Creates a test product and returns its ID
    /// </summary>
    protected async Task<Guid> CreateTestProductAsync(string name = "Test Product", decimal price = 99.99m, int stockQuantity = 100)
    {
        var product = new Product(name, price, stockQuantity);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();
        return product.Id;
    }

    /// <summary>
    /// Resets the database to a clean state
    /// </summary>
    protected async Task ResetDatabaseAsync()
    {
        await Factory.ResetDatabaseAsync();

        // Re-seed test data
        await SeedTestDataAsync();
    }

    // Helper class for auth response
    protected class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }
}
