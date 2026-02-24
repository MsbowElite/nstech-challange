using OrderService.Domain.Entities;

namespace OrderService.Domain.Specifications;

/// <summary>
/// Specification for querying orders within a date range.
/// </summary>
public class OrdersInDateRangeSpecification : Specification<Order>
{
    public OrdersInDateRangeSpecification(DateTime from, DateTime to)
    {
        Criteria = o => o.CreatedAt >= from && o.CreatedAt <= to;
        ApplyOrderByDescending(o => o.CreatedAt);
    }
}
