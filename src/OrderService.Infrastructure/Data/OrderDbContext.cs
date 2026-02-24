using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

namespace OrderService.Infrastructure.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(o => o.Id);
            
            entity.Property(o => o.CustomerId).IsRequired();
            
            // Configure OrderStatus value object conversion
            entity.Property(o => o.Status)
                .IsRequired()
                .HasConversion(
                    v => v.Name,                    // Convert to string for database
                    v => OrderStatus.FromName(v));  // Convert from string when reading
            
            entity.Property(o => o.Currency).IsRequired().HasMaxLength(3);
            entity.Property(o => o.CreatedAt).IsRequired();
            
            entity.OwnsMany(o => o.Items, items =>
            {
                items.ToTable("OrderItems");
                items.WithOwner().HasForeignKey("OrderId");
                items.Property<int>("Id").ValueGeneratedOnAdd();
                items.HasKey("Id");
                
                items.Property(i => i.ProductId).IsRequired();
                items.Property(i => i.UnitPrice).IsRequired().HasColumnType("decimal(18,2)");
                items.Property(i => i.Quantity).IsRequired();
            });

            entity.HasIndex(o => o.CustomerId);
            entity.HasIndex(o => o.Status);
            entity.HasIndex(o => o.CreatedAt);
            
            // Enable optimistic concurrency control with UpdatedAt
            entity.Property(o => o.UpdatedAt).IsConcurrencyToken();
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(p => p.Id);
            
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.UnitPrice).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(p => p.AvailableQuantity).IsRequired();
            entity.Property(p => p.CreatedAt).IsRequired();
        });

    }
}
