using Microsoft.EntityFrameworkCore;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Data;

/// <summary>
/// Read-only wrapper around OrderDbContext.
/// Provides AsNoTracking queries for optimal read performance.
/// Used by query handlers to avoid change tracking overhead.
/// </summary>
public class OrderDbContextReadOnly : IOrderDbContextReadOnly
{
    private readonly OrderDbContext _context;

    public OrderDbContextReadOnly(OrderDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the Orders DbSet as AsNoTracking for optimal query performance.
    /// </summary>
    public IQueryable<Order> Orders => _context.Orders.AsNoTracking();

    /// <summary>
    /// Gets the Products DbSet as AsNoTracking for optimal query performance.
    /// </summary>
    public IQueryable<Product> Products => _context.Products.AsNoTracking();
}
