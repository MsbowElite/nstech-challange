# Rich Domain Usage Examples

This document provides practical examples of how to use the new rich domain patterns in your Order Service.

## 1. Creating an Order (with Domain Events)

### Before (Anemic Model)
```csharp
// Application Layer - Had to handle validation
var order = new Order(customerId, currency, items);
// No events raised, external systems don't know about the order
await _orderRepository.AddAsync(order);
```

### After (Rich Domain)
```csharp
// Domain layer handles everything
var order = new Order(customerId, currency, items);  // Raises OrderCreatedEvent

// Application layer publishes events
await _orderRepository.AddAsync(order);
await _orderRepository.SaveChangesAsync();

var events = order.GetUncommittedEvents();
foreach (var evt in events)
{
    // Notify: Inventory Service, Notification Service, Analytics, etc.
    await _publisher.Publish(evt, cancellationToken);
}
order.ClearUncommittedEvents();
```

---

## 2. Working with Rich OrderStatus

### Before (Simple Enum)
```csharp
// Had to remember which statuses could transition
if (order.Status != OrderStatus.Placed)
    throw new InvalidOperationException("Invalid status");

if (order.Status == OrderStatus.Placed || order.Status == OrderStatus.Confirmed)
{
    // Can cancel
}
```

### After (Rich Value Object)
```csharp
// Status has behavior
if (!order.Status.CanTransitionToConfirmed())
    throw new InvalidOperationException("Cannot confirm");

if (order.Status.CanTransitionToCanceled())
{
    // Can cancel
}

// Get status name for display
var statusDisplay = order.Status.Name;  // "Confirmed"
var statusDescription = order.Status.Description;  // "Order has been confirmed"
```

---

## 3. Using Domain Service for Complex Validation

### Example: Confirming Order with Multiple Business Rules

```csharp
public class ConfirmOrderCommandHandler : IRequestHandler<ConfirmOrderCommand, OrderResponse>
{
    private readonly OrderDomainService _domainService;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    public async Task<OrderResponse> Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdForUpdateAsync(request.OrderId, cancellationToken);
        
        // Domain service validates complex rules
        if (!_domainService.CanConfirmOrder(order))
            throw new InvalidOperationException("Cannot confirm order");

        // Can check multiple conditions
        if (!_domainService.CanModifyOrder(order))
            throw new InvalidOperationException("Order cannot be modified");

        // Check inventory availability across multiple products
        var availableStock = await GetAvailableStockAsync(order.Items);
        if (!_domainService.HasSufficientStock(order, availableStock))
            throw new InvalidOperationException("Insufficient stock");

        // Check business rules (e.g., minimum order value)
        if (!_domainService.MeetsMinimumOrderValue(order, minimumValue: 50m))
            throw new InvalidOperationException("Order total below minimum");

        // All rules pass, proceed with confirmation
        order.Confirm();  // Raises OrderConfirmedEvent
        
        await _orderRepository.SaveChangesAsync(cancellationToken);
        await PublishEventsAsync(order, cancellationToken);
        
        return OrderMapper.MapToResponse(order);
    }
}
```

---

## 4. Publishing and Subscribing to Domain Events

### Creating an Event Subscriber

```csharp
// When OrderCreatedEvent is raised, this handler executes
public class SendOrderCreationNotificationHandler : 
    INotificationHandler<OrderCreatedEvent>
{
    private readonly INotificationService _notificationService;

    public SendOrderCreationNotificationHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Send notification to customer about order creation
        await _notificationService.SendOrderCreatedNotificationAsync(
            customerId: notification.CustomerId,
            orderId: notification.AggregateId,
            total: notification.Total,
            cancellationToken: cancellationToken
        );

        // Could also write to audit log, analytics, etc.
        // These are all decoupled from the order creation logic
    }
}

public class UpdateInventoryOnOrderConfirmedHandler : 
    INotificationHandler<OrderConfirmedEvent>
{
    private readonly IInventoryService _inventoryService;

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        // Reserve inventory when order is confirmed
        await _inventoryService.ReserveInventoryAsync(
            orderId: notification.AggregateId,
            customerId: notification.CustomerId,
            confirmedAt: notification.ConfirmedAt,
            cancellationToken: cancellationToken
        );
    }
}

public class SendOrderCancellationNotificationHandler : 
    INotificationHandler<OrderCanceledEvent>
{
    private readonly INotificationService _notificationService;

    public async Task Handle(OrderCanceledEvent notification, CancellationToken cancellationToken)
    {
        // Notify customer about cancellation with reason
        await _notificationService.SendOrderCancelledNotificationAsync(
            customerId: notification.CustomerId,
            orderId: notification.AggregateId,
            reason: notification.Reason,
            cancellationToken: cancellationToken
        );
    }
}
```

### Registering Event Handlers in DI

```csharp
// In Program.cs
services.AddMediatR(config =>
{
    config.RegisterServicesFromAssemblies(
        typeof(Program).Assembly,
        typeof(OrderService.Domain.Entities.Order).Assembly
    );
});

// Register specific event handlers
services.AddTransient<
    INotificationHandler<OrderCreatedEvent>, 
    SendOrderCreationNotificationHandler>();

services.AddTransient<
    INotificationHandler<OrderConfirmedEvent>, 
    UpdateInventoryOnOrderConfirmedHandler>();

services.AddTransient<
    INotificationHandler<OrderCanceledEvent>, 
    SendOrderCancellationNotificationHandler>();
```

---

## 5. Using Specifications for Queries

### Example: Complex Order Queries

```csharp
public class OrderQueryService
{
    private readonly IOrderRepository _repository;

    // Get single order using specification
    public async Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken ct)
    {
        var spec = new OrderByIdSpecification(orderId);
        var order = await _repository.GetBySpecificationAsync(spec, ct);
        return order != null ? OrderMapper.MapToResponse(order) : null;
    }

    // Get all orders for a customer
    public async Task<List<OrderResponse>> GetCustomerOrdersAsync(
        Guid customerId, CancellationToken ct)
    {
        var spec = new OrderByCustomerIdSpecification(customerId);
        var orders = await _repository.GetBySpecificationAsync(spec, ct);
        return orders?.Select(OrderMapper.MapToResponse).ToList() ?? new();
    }

    // Get all confirmed orders (for payment processing)
    public async Task<List<OrderResponse>> GetConfirmedOrdersAsync(CancellationToken ct)
    {
        var spec = new OrdersByStatusSpecification(OrderStatus.Confirmed);
        var orders = await _repository.GetBySpecificationAsync(spec, ct);
        return orders?.Select(OrderMapper.MapToResponse).ToList() ?? new();
    }

    // Get orders within date range (for reporting)
    public async Task<List<OrderResponse>> GetOrdersInDateRangeAsync(
        DateTime from, DateTime to, CancellationToken ct)
    {
        var spec = new OrdersInDateRangeSpecification(from, to);
        var orders = await _repository.GetBySpecificationAsync(spec, ct);
        return orders?.Select(OrderMapper.MapToResponse).ToList() ?? new();
    }
}
```

---

## 6. Testing Rich Domain Logic

### Unit Tests (No Database Required)

```csharp
public class OrderAggregateTests
{
    [Fact]
    public void CreateOrder_WithValidData_RaisesOrderCreatedEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var items = new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), 10m, 2)
        };

        // Act
        var order = new Order(customerId, "USD", items);

        // Assert
        var events = order.GetUncommittedEvents();
        Assert.Single(events);
        Assert.IsType<OrderCreatedEvent>(events.First());
        Assert.Equal(20m, order.Total);
    }

    [Fact]
    public void ConfirmOrder_FromPlacedStatus_Succeeds()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        order.Confirm();

        // Assert
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        var events = order.GetUncommittedEvents();
        Assert.Contains(events, e => e is OrderConfirmedEvent);
    }

    [Fact]
    public void ConfirmOrder_FromConfirmedStatus_ThrowsException()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Confirm();
        order.ClearUncommittedEvents();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.Confirm());
    }

    [Fact]
    public void CancelOrder_FromPlacedStatus_Succeeds()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        order.Cancel("Customer requested cancellation");

        // Assert
        Assert.Equal(OrderStatus.Canceled, order.Status);
        var events = order.GetUncommittedEvents();
        var cancelEvent = events.OfType<OrderCanceledEvent>().First();
        Assert.Equal("Customer requested cancellation", cancelEvent.Reason);
    }

    [Fact]
    public void CancelOrder_FromCanceledStatus_ThrowsException()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Cancel();
        order.ClearUncommittedEvents();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.Cancel());
    }

    private static Order CreateTestOrder()
    {
        var items = new List<OrderItem> 
        { 
            new OrderItem(Guid.NewGuid(), 100m, 1) 
        };
        return new Order(Guid.NewGuid(), "USD", items);
    }
}
```

### Domain Service Tests

```csharp
public class OrderDomainServiceTests
{
    private readonly OrderDomainService _service = new();

    [Fact]
    public void CanConfirmOrder_WithPlacedStatus_ReturnsTrue()
    {
        // Arrange
        var order = CreateTestOrder(OrderStatus.Placed);

        // Act
        var result = _service.CanConfirmOrder(order);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanConfirmOrder_WithConfirmedStatus_ReturnsFalse()
    {
        // Arrange
        var order = CreateTestOrder(OrderStatus.Confirmed);

        // Act
        var result = _service.CanConfirmOrder(order);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasSufficientStock_WithEnoughInventory_ReturnsTrue()
    {
        // Arrange
        var order = CreateTestOrder();
        var availableStock = new Dictionary<Guid, int>
        {
            { order.Items.First().ProductId, 10 }
        };

        // Act
        var result = _service.HasSufficientStock(order, availableStock);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasSufficientStock_WithInsufficientInventory_ReturnsFalse()
    {
        // Arrange
        var order = CreateTestOrder();
        var availableStock = new Dictionary<Guid, int>
        {
            { order.Items.First().ProductId, 1 }
        };

        // Act
        var result = _service.HasSufficientStock(order, availableStock);

        // Assert
        Assert.False(result);
    }

    private Order CreateTestOrder(OrderStatus? status = null)
    {
        var items = new List<OrderItem> 
        { 
            new OrderItem(Guid.NewGuid(), 100m, 5) 
        };
        return new Order(Guid.NewGuid(), "USD", items);
    }
}
```

---

## 7. Extending the Domain

### Adding a New Business Rule

```csharp
// Extend OrderDomainService with new rule
public class OrderDomainService
{
    // ... existing methods ...

    /// <summary>
    /// Validates if order matches customer's preferred payment method.
    /// </summary>
    public bool IsPaymentMethodPreferred(Order order, Guid customerId, PaymentMethod method)
    {
        // Custom business logic
        return true;
    }

    /// <summary>
    /// Calculates applicable discounts based on order value and customer.
    /// </summary>
    public decimal CalculateDiscount(Order order, Guid customerId)
    {
        if (order.Total >= 100)
            return order.Total * 0.05m; // 5% discount
        return 0;
    }
}
```

### Adding a New Domain Event

```csharp
// New event for tracking
public class OrderItemAddedEvent : DomainEvent
{
    public OrderItem Item { get; }

    public OrderItemAddedEvent(Guid orderId, OrderItem item) : base(orderId)
    {
        Item = item;
    }
}

// Modify Order aggregate to raise event
public class Order : IAggregateRoot
{
    public void AddItem(OrderItem item)
    {
        // Validation
        if (!Status.CanTransitionToConfirmed())
            throw new InvalidOperationException("Cannot add items to this order");

        _items.Add(item);
        _uncommittedEvents.Add(new OrderItemAddedEvent(Id, item));
    }
}
```

---

## 8. Best Practices

### ✅ DO

- Keep domain logic in aggregate roots and domain services
- Use value objects for domain concepts (OrderStatus, OrderItem)
- Raise domain events for important business events
- Use specifications for query logic
- Test domain logic without dependencies
- Use domain service for cross-aggregate validation

### ❌ DON'T

- Put business logic in application handlers
- Modify aggregates from multiple places
- Create domain events in application layer
- Use simple primitives for domain concepts
- Bypass aggregate root to access parts
- Mix query logic across handlers

---

## Summary

The rich domain implementation provides:

1. **Clear Intent** - Code expresses business rules
2. **Testability** - Domain logic tests don't need infrastructure
3. **Reusability** - Specifications and services are reused
4. **Maintainability** - Changes to rules are centralized
5. **Scalability** - Events enable integration with other services
6. **Type Safety** - Value objects prevent invalid states

This is how professional, maintainable microservices handle business logic!
