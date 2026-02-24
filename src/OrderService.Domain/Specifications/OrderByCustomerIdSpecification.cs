using OrderService.Domain.Entities;

namespace OrderService.Domain.Specifications;

/// <summary>
/// Specification for querying orders by customer ID.
/// </summary>
public class OrderByCustomerIdSpecification : Specification<Order>
{
    public OrderByCustomerIdSpecification(Guid customerId)
    {
        Criteria = o => o.CustomerId == customerId;
        ApplyOrderByDescending(o => o.CreatedAt);
    }
}
