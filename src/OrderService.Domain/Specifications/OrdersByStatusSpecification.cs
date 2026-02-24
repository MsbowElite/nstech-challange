using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Specifications;

/// <summary>
/// Specification for querying orders by status.
/// </summary>
public class OrdersByStatusSpecification : Specification<Order>
{
    public OrdersByStatusSpecification(OrderStatus status)
    {
        Criteria = o => o.Status == status;
        ApplyOrderByDescending(o => o.CreatedAt);
    }
}
