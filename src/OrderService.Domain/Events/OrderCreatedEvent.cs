namespace OrderService.Domain.Events;

/// <summary>
/// Raised when a new order is created.
/// </summary>
public class OrderCreatedEvent : DomainEvent
{
    public Guid CustomerId { get; }
    public string Currency { get; }
    public decimal Total { get; }
    public DateTime CreatedAt { get; }

    public OrderCreatedEvent(Guid orderId, Guid customerId, string currency, decimal total, DateTime createdAt)
        : base(orderId)
    {
        CustomerId = customerId;
        Currency = currency;
        Total = total;
        CreatedAt = createdAt;
    }
}
