using OrderService.Domain.Events;
using OrderService.Domain.Interfaces;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

/// <summary>
/// Order aggregate root.
/// Represents a customer's order containing multiple items.
/// Implements the aggregate root pattern and raises domain events to notify interested parties of changes.
/// </summary>
public class Order : IAggregateRoot
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private readonly List<DomainEvent> _uncommittedEvents = new();

    public decimal Total => _items.Sum(item => item.Subtotal);

    private Order() 
    {
        Status = OrderStatus.Draft; // Default for EF Core
    }

    public Order(Guid customerId, string currency, List<OrderItem> items)
    {
        ValidateOrder(customerId, currency, items);

        Id = Guid.NewGuid();
        CustomerId = customerId;
        Currency = currency;
        Status = OrderStatus.Placed;
        _items = items;
        CreatedAt = DateTime.UtcNow;

        // Raise domain event
        _uncommittedEvents.Add(new OrderCreatedEvent(Id, CustomerId, Currency, Total, CreatedAt));
    }

    /// <summary>
    /// Confirms the order, transitioning it from Placed to Confirmed status.
    /// Only orders in Placed status can be confirmed.
    /// </summary>
    public void Confirm()
    {
        if (!Status.CanTransitionToConfirmed())
            throw new InvalidOperationException($"Cannot confirm order in {Status.Name} status. Only Placed orders can be confirmed.");

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;

        // Raise domain event
        _uncommittedEvents.Add(new OrderConfirmedEvent(Id, CustomerId, UpdatedAt.Value));
    }

    /// <summary>
    /// Cancels the order, transitioning it to Canceled status.
    /// Only orders in Placed or Confirmed status can be canceled.
    /// </summary>
    public void Cancel(string? reason = null)
    {
        if (!Status.CanTransitionToCanceled())
            throw new InvalidOperationException($"Cannot cancel order in {Status.Name} status. Only Placed or Confirmed orders can be canceled.");

        Status = OrderStatus.Canceled;
        UpdatedAt = DateTime.UtcNow;

        // Raise domain event
        _uncommittedEvents.Add(new OrderCanceledEvent(Id, CustomerId, UpdatedAt.Value, reason));
    }

    /// <summary>
    /// Query method: checks if order is confirmed.
    /// </summary>
    public bool IsConfirmed() => Status == OrderStatus.Confirmed;

    /// <summary>
    /// Query method: checks if order is canceled.
    /// </summary>
    public bool IsCanceled() => Status == OrderStatus.Canceled;

    /// <summary>
    /// Query method: checks if order is in placed state.
    /// </summary>
    public bool IsPlaced() => Status == OrderStatus.Placed;

    /// <summary>
    /// Query method: checks if order can transition to confirmed state.
    /// </summary>
    public bool CanBeConfirmed() => Status.CanTransitionToConfirmed();

    /// <summary>
    /// Query method: checks if order can transition to canceled state.
    /// </summary>
    public bool CanBeCanceled() => Status.CanTransitionToCanceled();

    /// <summary>
    /// Gets all uncommitted domain events that were raised by this aggregate.
    /// </summary>
    public IReadOnlyCollection<DomainEvent> GetUncommittedEvents() => _uncommittedEvents.AsReadOnly();

    /// <summary>
    /// Clears all uncommitted domain events after they have been processed/persisted.
    /// </summary>
    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    private static void ValidateOrder(Guid customerId, string currency, List<OrderItem> items)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID is required", nameof(customerId));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required", nameof(currency));
        if (items == null || !items.Any())
            throw new ArgumentException("Order must have at least one item", nameof(items));
    }
}
