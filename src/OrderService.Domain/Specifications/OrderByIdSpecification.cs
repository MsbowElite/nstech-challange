using OrderService.Domain.Entities;

namespace OrderService.Domain.Specifications;

/// <summary>
/// Specification for querying a single order by ID.
/// </summary>
public class OrderByIdSpecification : Specification<Order>
{
    public OrderByIdSpecification(Guid orderId)
    {
        Criteria = o => o.Id == orderId;
    }
}
