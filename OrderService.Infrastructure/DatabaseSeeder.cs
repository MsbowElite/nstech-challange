using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(OrderDbContext context)
    {
        // Check if products already exist
        if (await context.Products.AnyAsync())
            return;

        var products = new List<Product>
        {
            new Product("Laptop Dell XPS 15", 1299.99m, 50),
            new Product("Mouse Logitech MX Master 3", 99.99m, 200),
            new Product("Keyboard Mechanical RGB", 149.99m, 100),
            new Product("Monitor LG 27 4K", 399.99m, 30),
            new Product("Webcam Logitech C920", 79.99m, 150),
            new Product("Headset Sony WH-1000XM4", 349.99m, 75),
            new Product("USB-C Hub Multiport", 49.99m, 300),
            new Product("External SSD 1TB", 129.99m, 120)
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}
