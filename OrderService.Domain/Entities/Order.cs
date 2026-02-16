using OrderService.Domain.Enums;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public string Currency { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal Total => _items.Sum(item => item.Subtotal);

    private Order() { }

    public Order(Guid customerId, string currency, List<OrderItem> items)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID is required", nameof(customerId));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required", nameof(currency));
        if (items == null || !items.Any())
            throw new ArgumentException("Order must have at least one item", nameof(items));

        Id = Guid.NewGuid();
        CustomerId = customerId;
        Currency = currency;
        Status = OrderStatus.Placed;
        _items = items;
        CreatedAt = DateTime.UtcNow;
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Placed)
            throw new InvalidOperationException($"Cannot confirm order in {Status} status. Only Placed orders can be confirmed.");

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status != OrderStatus.Placed && Status != OrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot cancel order in {Status} status. Only Placed or Confirmed orders can be canceled.");

        Status = OrderStatus.Canceled;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsConfirmed() => Status == OrderStatus.Confirmed;
    public bool IsCanceled() => Status == OrderStatus.Canceled;
    public bool CanBeConfirmed() => Status == OrderStatus.Placed;
    public bool CanBeCanceled() => Status == OrderStatus.Placed || Status == OrderStatus.Confirmed;
}
