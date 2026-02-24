using OrderService.Domain.Entities;

namespace OrderService.Application.Interfaces;

/// <summary>
/// Read-only context interface for query operations.
/// Provides AsNoTracking queries for optimal read performance.
/// No change tracking overhead for query scenarios.
/// </summary>
public interface IOrderDbContextReadOnly
{
    IQueryable<Order> Orders { get; }
    IQueryable<Product> Products { get; }
}
