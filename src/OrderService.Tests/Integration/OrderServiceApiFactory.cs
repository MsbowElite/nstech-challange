using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderService.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace OrderService.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that sets up the API with a PostgreSQL test container
/// This allows integration tests to use the real production dependency injection setup
/// </summary>
public class OrderServiceApiFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _dbContainer;

    public OrderServiceApiFactory()
    {
        // Configure PostgreSQL container
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("orderservicedb_test")
            .WithUsername("testuser")
            .WithPassword("testpass123")
            .WithCleanUp(true)
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext registration
            services.RemoveAll(typeof(DbContextOptions<OrderDbContext>));
            services.RemoveAll(typeof(OrderDbContext));

            // Add DbContext with test container connection string
            services.AddDbContext<OrderDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            // Ensure database is created and migrated
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            context.Database.Migrate();
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Starts the PostgreSQL container. Call this before using the factory.
    /// </summary>
    public async Task StartAsync()
    {
        await _dbContainer.StartAsync();
    }

    /// <summary>
    /// Stops and cleans up the PostgreSQL container.
    /// </summary>
    public async Task StopAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    /// <summary>
    /// Gets the OrderDbContext for direct database access in tests
    /// </summary>
    public OrderDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    }

    /// <summary>
    /// Resets the database to a clean state between tests
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        // Clear all data
        context.Orders.RemoveRange(context.Orders);
        context.Products.RemoveRange(context.Products);
        await context.SaveChangesAsync();
    }
}
