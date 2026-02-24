namespace OrderService.Domain.Events;

/// <summary>
/// Raised when an order is confirmed.
/// </summary>
public class OrderConfirmedEvent : DomainEvent
{
    public Guid CustomerId { get; }
    public DateTime ConfirmedAt { get; }

    public OrderConfirmedEvent(Guid orderId, Guid customerId, DateTime confirmedAt)
        : base(orderId)
    {
        CustomerId = customerId;
        ConfirmedAt = confirmedAt;
    }
}
