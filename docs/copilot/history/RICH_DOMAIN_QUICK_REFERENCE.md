# Rich Domain Quick Reference

## Key Concepts at a Glance

### 1. Aggregate Root (Order)
- **What**: Entity that enforces consistency for a group of objects
- **Where**: `src/OrderService.Domain/Entities/Order.cs`
- **Implements**: `IAggregateRoot`
- **Methods**: `Confirm()`, `Cancel()`, `GetUncommittedEvents()`

### 2. Domain Events
- **What**: Immutable records of business actions
- **Where**: `src/OrderService.Domain/Events/`
- **Types**:
  - `OrderCreatedEvent` - Order was created
  - `OrderConfirmedEvent` - Order was confirmed
  - `OrderCanceledEvent` - Order was canceled

### 3. Rich Value Objects
- **What**: Objects that represent domain concepts
- **Where**: `src/OrderService.Domain/ValueObjects/`
- **Key**: `OrderStatus` - Replaces simple enum
  - `CanTransitionToConfirmed()`, `CanTransitionToCanceled()`
  - `IsTerminal()`

### 4. Domain Services
- **What**: Operations that don't belong to a single entity
- **Where**: `src/OrderService.Domain/Services/OrderDomainService.cs`
- **Methods**:
  - `CanConfirmOrder(order)`
  - `CanCancelOrder(order)`
  - `HasSufficientStock(order, stock)`
  - `MeetsMinimumOrderValue(order, minimum)`

### 5. Specifications
- **What**: Encapsulate query logic
- **Where**: `src/OrderService.Domain/Specifications/`
- **Available**:
  - `OrderByIdSpecification`
  - `OrderByCustomerIdSpecification`
  - `OrdersByStatusSpecification`
  - `OrdersInDateRangeSpecification`

---

## File Structure

```
Domain/
├── Events/                          # Domain events
│   ├── DomainEvent.cs              # Base class
│   ├── OrderCreatedEvent.cs
│   ├── OrderConfirmedEvent.cs
│   └── OrderCanceledEvent.cs
├── Entities/
│   └── Order.cs                    # Aggregate root (MODIFIED)
├── ValueObjects/
│   ├── OrderStatus.cs              # Rich value object (NEW)
│   └── OrderItem.cs
├── Interfaces/
│   └── IAggregateRoot.cs           # Aggregate root contract (NEW)
├── Services/
│   └── OrderDomainService.cs       # Domain service (NEW)
├── Specifications/
│   ├── Specification.cs            # Base specification (NEW)
│   ├── OrderByIdSpecification.cs
│   ├── OrderByCustomerIdSpecification.cs
│   ├── OrdersByStatusSpecification.cs
│   └── OrdersInDateRangeSpecification.cs
└── Enums/ 
    └── (OrderStatus.cs deleted - replaced by ValueObjects/OrderStatus.cs)

Application/
├── Commands/
│   ├── OrderCommands.cs
│   └── Handlers/
│       ├── CreateOrderCommandHandler.cs (MODIFIED)
│       ├── ConfirmOrderCommandHandler.cs (MODIFIED)
│       └── CancelOrderCommandHandler.cs (MODIFIED)
├── Queries/
│   └── Handlers/
│       └── OrderQueryHandlers.cs (MODIFIED)
├── DTOs/
│   └── OrderDTOs.cs
├── Interfaces/
│   └── IRepositories.cs (MODIFIED - added GetBySpecificationAsync)
└── Mappers/
    └── OrderMapper.cs (NEW)
```

---

## Quick Implementation Checklist

### To Implement `GetBySpecificationAsync` in your Repository:

```csharp
public async Task<Order?> GetBySpecificationAsync(
    Specification<Order> spec, 
    CancellationToken cancellationToken = default)
{
    var query = _dbContext.Orders.AsQueryable();
    
    // Apply filtering
    if (spec.Criteria != null)
        query = query.Where(spec.Criteria);
    
    // Apply includes
    query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));
    
    return await query.FirstOrDefaultAsync(cancellationToken);
}
```

### To Register Services in DI:

```csharp
// Program.cs
services.AddScoped<OrderDomainService>();
services.AddScoped<IOrderRepository, OrderRepository>();
services.AddScoped<IProductRepository, ProductRepository>();

// MediatR will automatically discover event handlers
services.AddMediatR(config => 
{
    config.RegisterServicesFromAssemblies(typeof(Program).Assembly);
});
```

---

## Common Patterns

### Creating an Order with Events
```csharp
var order = new Order(customerId, currency, items);  // Raises OrderCreatedEvent
await _repository.AddAsync(order);
await _repository.SaveChangesAsync();

// Publish events
var events = order.GetUncommittedEvents();
foreach (var evt in events) await _publisher.Publish(evt);
order.ClearUncommittedEvents();
```

### Confirming an Order
```csharp
var order = await _repository.GetByIdAsync(orderId);

if (!_domainService.CanConfirmOrder(order))
    throw new InvalidOperationException("Cannot confirm");

order.Confirm();  // Raises OrderConfirmedEvent
await _repository.SaveChangesAsync();
```

### Querying Orders
```csharp
var spec = new OrderByCustomerIdSpecification(customerId);
var orders = await _repository.GetBySpecificationAsync(spec);
```

### Subscribing to Events
```csharp
public class MyEventHandler : INotificationHandler<OrderConfirmedEvent>
{
    public async Task Handle(OrderConfirmedEvent evt, CancellationToken ct)
    {
        // React to event
    }
}
```

---

## Status Values

```
OrderStatus.Draft       (0) - Initial state
OrderStatus.Placed      (1) - Order placed by customer
OrderStatus.Confirmed   (2) - Order confirmed & stock reserved
OrderStatus.Canceled    (3) - Order canceled (terminal)
```

### Allowed Transitions
```
Draft → Placed
Placed → Confirmed
Placed → Canceled
Confirmed → Canceled
```

---

## Key Methods Reference

### Order Aggregate
```csharp
order.Confirm()                           // Transition to Confirmed
order.Cancel(reason)                      // Transition to Canceled
order.IsConfirmed()                       // Check status
order.CanBeConfirmed()                    // Can transition?
order.GetUncommittedEvents()              // Get raised events
order.ClearUncommittedEvents()            // Clear events after publish
```

### OrderStatus Value Object
```csharp
order.Status.CanTransitionToConfirmed()   // Can confirm?
order.Status.CanTransitionToCanceled()    // Can cancel?
order.Status.IsTerminal()                 // Is final state?
order.Status.Name                         // Get status name
order.Status.Description                  // Get description
```

### OrderDomainService
```csharp
_domainService.CanConfirmOrder(order)
_domainService.CanCancelOrder(order)
_domainService.CanModifyOrder(order)
_domainService.HasSufficientStock(order, availableStock)
_domainService.MeetsMinimumOrderValue(order, minimumValue)
```

### Specifications
```csharp
new OrderByIdSpecification(orderId)
new OrderByCustomerIdSpecification(customerId)
new OrdersByStatusSpecification(OrderStatus.Confirmed)
new OrdersInDateRangeSpecification(from, to)
```

---

## Documentation Files

- **`RICH_DOMAIN_IMPLEMENTATION.md`** - Complete implementation guide
- **`RICH_DOMAIN_CHANGES.md`** - Detailed list of all changes
- **`RICH_DOMAIN_EXAMPLES.md`** - Practical code examples
- **`RICH_DOMAIN_QUICK_REFERENCE.md`** - This file

---

## Troubleshooting

### Problem: `'GetBySpecificationAsync' not found`
**Solution**: Implement `GetBySpecificationAsync` in your repository

### Problem: `OrderDomainService` not injected
**Solution**: Register in DI: `services.AddScoped<OrderDomainService>()`

### Problem: Events not being handled
**Solution**: Create INotificationHandler<YourEvent> and register in DI

### Problem: `OrderStatus enum not found`
**Solution**: Use `OrderStatus.Placed` instead of `OrderStatus.Placed`

### Problem: Status comparison not working
**Solution**: Use `order.Status == OrderStatus.Confirmed` (uses overloaded equals)

---

## What Changed for Me?

### If I'm writing handlers:
1. Use `OrderDomainService` for validation
2. Publish domain events after state changes
3. Use `OrderMapper` for DTO conversion

### If I'm querying:
1. Use `Specification` pattern for complex queries
2. Call `GetBySpecificationAsync()` instead of custom methods

### If I'm testing:
1. Test domain logic directly without DB
2. Mock `IPublisher` to verify events
3. Use `OrderDomainService` for business rule testing

### If I'm subscribing to events:
1. Create `INotificationHandler<OrderXxxEvent>`
2. Implement your business logic
3. Register in DI container

---

## Performance Impact

- **Minimal**: Events stored in memory until cleared
- **Specifications**: Same as direct LINQ queries
- **Domain Service**: Fast (no I/O)
- **Mapper**: Highly optimized by MediatR

---

## Next Steps

1. ✅ Review the implementation
2. ✅ Update Repository implementation
3. ✅ Register services in DI
4. ✅ Create event handlers
5. ✅ Update integration tests
6. ✅ Deploy with confidence!

---

**Questions?** See `RICH_DOMAIN_EXAMPLES.md` for code samples
