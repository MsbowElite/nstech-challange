namespace OrderService.Domain.Events;

/// <summary>
/// Raised when an order is canceled.
/// </summary>
public class OrderCanceledEvent : DomainEvent
{
    public Guid CustomerId { get; }
    public string? Reason { get; }
    public DateTime CanceledAt { get; }

    public OrderCanceledEvent(Guid orderId, Guid customerId, DateTime canceledAt, string? reason = null)
        : base(orderId)
    {
        CustomerId = customerId;
        Reason = reason;
        CanceledAt = canceledAt;
    }
}
