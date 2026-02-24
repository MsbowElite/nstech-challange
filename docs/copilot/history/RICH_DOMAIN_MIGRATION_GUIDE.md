# Rich Domain Migration & Integration Guide

This guide helps you integrate the rich domain implementation with your existing infrastructure code.

## Phase 1: Update Repository Implementation

### Update OrderRepository

Your `OrderRepository` implementation needs to support the new specification pattern:

```csharp
using OrderService.Domain.Entities;
using OrderService.Domain.Specifications;
using OrderService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly YourDbContext _context;

    public OrderRepository(YourDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<Order?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    // NEW METHOD - Required for specification pattern
    public async Task<Order?> GetBySpecificationAsync(
        Specification<Order> spec, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsQueryable();

        // Apply filtering criteria
        if (spec.Criteria != null)
            query = query.Where(spec.Criteria);

        // Apply string includes (relationships)
        query = spec.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        // Apply expression includes (relationships)
        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

        // Apply ordering
        if (spec.OrderBy != null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending != null)
            query = query.OrderByDescending(spec.OrderByDescending);

        // Apply paging
        if (spec.IsPagingEnabled)
        {
            if (spec.Skip.HasValue)
                query = query.Skip(spec.Skip.Value);

            if (spec.Take.HasValue)
                query = query.Take(spec.Take.Value);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(List<Order> Orders, int TotalCount)> GetPagedAsync(
        Guid? customerId,
        string? status,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .Include(o => o.Items)
            .AsQueryable();

        // Apply filters
        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status.Name == status);

        if (from.HasValue)
            query = query.Where(o => o.CreatedAt >= from);

        if (to.HasValue)
            query = query.Where(o => o.CreatedAt <= to);

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (orders, totalCount);
    }

    public async Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
        return order;
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Update(order);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

---

## Phase 2: Update Dependency Injection

### Program.cs - Service Registration

```csharp
// Add the following to your Program.cs

// Domain Services
services.AddScoped<OrderService.Domain.Services.OrderDomainService>();

// Repositories
services.AddScoped<IOrderRepository, OrderRepository>();
services.AddScoped<IProductRepository, ProductRepository>();

// MediatR - for CQRS commands/queries and domain event publishing
services.AddMediatR(config =>
{
    config.RegisterServicesFromAssemblies(
        typeof(Program).Assembly,  // Application handlers
        typeof(OrderService.Domain.Entities.Order).Assembly  // Domain events
    );
});

// Optional: Event handlers (register specific ones if not using auto-discovery)
services.AddTransient<INotificationHandler<OrderService.Domain.Events.OrderCreatedEvent>, 
    YourOrderCreatedEventHandler>();
services.AddTransient<INotificationHandler<OrderService.Domain.Events.OrderConfirmedEvent>, 
    YourOrderConfirmedEventHandler>();
services.AddTransient<INotificationHandler<OrderService.Domain.Events.OrderCanceledEvent>, 
    YourOrderCanceledEventHandler>();
```

---

## Phase 3: Create Event Handlers

### Example Event Handlers

Create these in your application or infrastructure layer:

```csharp
// src/OrderService.Application/EventHandlers/OrderEventHandlers.cs

using MediatR;
using OrderService.Domain.Events;
using OrderService.Application.Interfaces;

namespace OrderService.Application.EventHandlers;

/// <summary>
/// Handles OrderCreatedEvent - notifies customers about their new order
/// </summary>
public class SendOrderCreationNotificationHandler : 
    INotificationHandler<OrderCreatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<SendOrderCreationNotificationHandler> _logger;

    public SendOrderCreationNotificationHandler(
        INotificationService notificationService,
        ILogger<SendOrderCreationNotificationHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Sending order creation notification for order {OrderId} to customer {CustomerId}",
                notification.AggregateId, notification.CustomerId);

            await _notificationService.SendOrderCreatedNotificationAsync(
                customerId: notification.CustomerId,
                orderId: notification.AggregateId,
                total: notification.Total,
                currency: notification.Currency,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Order creation notification sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending order creation notification");
            // Don't throw - let order persist even if notification fails
        }
    }
}

/// <summary>
/// Handles OrderConfirmedEvent - reserves inventory
/// </summary>
public class ReserveInventoryOnOrderConfirmedHandler : 
    INotificationHandler<OrderConfirmedEvent>
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ReserveInventoryOnOrderConfirmedHandler> _logger;

    public ReserveInventoryOnOrderConfirmedHandler(
        IInventoryService inventoryService,
        ILogger<ReserveInventoryOnOrderConfirmedHandler> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Reserving inventory for order {OrderId} confirmed at {ConfirmedAt}",
                notification.AggregateId, notification.ConfirmedAt);

            await _inventoryService.ReserveInventoryAsync(
                orderId: notification.AggregateId,
                customerId: notification.CustomerId,
                confirmedAt: notification.ConfirmedAt,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Inventory reserved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving inventory");
            // Log but don't throw - inventory service may be async
        }
    }
}

/// <summary>
/// Handles OrderCanceledEvent - releases inventory and notifies customer
/// </summary>
public class HandleOrderCancellationHandler : 
    INotificationHandler<OrderCanceledEvent>
{
    private readonly IInventoryService _inventoryService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<HandleOrderCancellationHandler> _logger;

    public HandleOrderCancellationHandler(
        IInventoryService inventoryService,
        INotificationService notificationService,
        ILogger<HandleOrderCancellationHandler> logger)
    {
        _inventoryService = inventoryService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(OrderCanceledEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Handling cancellation for order {OrderId}. Reason: {Reason}",
                notification.AggregateId, notification.Reason ?? "Not provided");

            // Release inventory
            await _inventoryService.ReleaseInventoryAsync(
                orderId: notification.AggregateId,
                cancellationToken: cancellationToken);

            // Notify customer
            await _notificationService.SendOrderCancelledNotificationAsync(
                customerId: notification.CustomerId,
                orderId: notification.AggregateId,
                reason: notification.Reason,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Order cancellation handled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling order cancellation");
        }
    }
}
```

---

## Phase 4: Update DbContext (if using EF Core)

### Adding Event Tracking to DbContext

If you want to persist events (for audit trail):

```csharp
public class OrderServiceDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
    
    // Optional: Store domain events
    public DbSet<StoredDomainEvent> DomainEvents { get; set; }

    public OrderServiceDbContext(DbContextOptions<OrderServiceDbContext> options) 
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Order entity
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status)
                .HasConversion(
                    v => v.Name,
                    v => OrderStatus.FromName(v));
        });

        // Ignore domain events (in-memory only)
        modelBuilder.Entity<Order>()
            .Ignore(o => o.GetUncommittedEvents());
    }

    // Optional: Save domain events before committing
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Persist domain events if implementing event sourcing
        
        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

---

## Phase 5: Update Integration Tests

### Testing with Events

```csharp
using Xunit;
using Moq;
using MediatR;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using OrderService.Application.Commands;
using OrderService.Application.Interfaces;

namespace OrderService.Tests.Integration;

public class OrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IPublisher> _publisherMock;

    public OrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _publisherMock = new Mock<IPublisher>();
    }

    [Fact]
    public async Task CreateOrderCommand_RaisesOrderCreatedEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var request = new CreateOrderRequest(
            customerId,
            "USD",
            new List<OrderItemRequest> { new(productId, 1) }
        );

        var product = new Product { Id = productId, UnitPrice = 100m, AvailableQuantity = 10 };
        _productRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var handler = new CreateOrderCommandHandler(
            _orderRepositoryMock.Object,
            _productRepositoryMock.Object,
            _publisherMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateOrderCommand(request),
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        
        // Verify event was published
        _publisherMock.Verify(
            p => p.Publish(
                It.IsAny<OrderCreatedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmOrderCommand_RaisesOrderConfirmedEvent()
    {
        // Arrange
        var order = CreateTestOrder();
        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = new ConfirmOrderCommandHandler(
            _orderRepositoryMock.Object,
            _productRepositoryMock.Object,
            new OrderDomainService(),
            _publisherMock.Object);

        // Act
        var result = await handler.Handle(
            new ConfirmOrderCommand(order.Id, Guid.NewGuid().ToString()),
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        
        // Verify event was published
        _publisherMock.Verify(
            p => p.Publish(
                It.IsAny<OrderConfirmedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Order CreateTestOrder()
    {
        var items = new List<OrderItem> { new(Guid.NewGuid(), 100m, 1) };
        return new Order(Guid.NewGuid(), "USD", items);
    }
}
```

---

## Phase 6: Testing Checklist

- [ ] OrderRepository.GetBySpecificationAsync() implemented
- [ ] OrderDomainService registered in DI
- [ ] Event handlers created and registered
- [ ] MediatR configured for event publishing
- [ ] Integration tests updated
- [ ] Domain logic tests passing
- [ ] Event publishing verified in tests
- [ ] DbContext configuration updated
- [ ] Migrations created (if using EF Core)
- [ ] Application compiles without errors

---

## Phase 7: Deployment Checklist

- [ ] All code reviews completed
- [ ] Integration tests pass
- [ ] Performance tests run
- [ ] Documentation updated
- [ ] Rollback plan prepared
- [ ] Event handler deployment ordered
- [ ] Logging configured for events
- [ ] Monitoring configured for events
- [ ] Backup taken before deployment
- [ ] Deployment completed successfully

---

## Common Issues & Solutions

### Issue 1: `GetBySpecificationAsync` not found
**Solution**: Implement the method in OrderRepository (see Phase 1)

### Issue 2: `OrderDomainService` not found in DI
**Solution**: Add to Program.cs: `services.AddScoped<OrderDomainService>()`

### Issue 3: Events not being published
**Solution**: Verify `IPublisher` is injected and `await _publisher.Publish(evt)`

### Issue 4: Event handlers not executing
**Solution**: Verify handlers are registered in DI and inherit from `INotificationHandler<T>`

### Issue 5: Status comparison fails
**Solution**: Use `order.Status == OrderStatus.Confirmed` (equals is overridden)

### Issue 6: Old enum style status not working
**Solution**: Replace `OrderStatus.Placed` with `OrderStatus.Placed` (same syntax, different type)

---

## Monitoring & Logging

### Add structured logging to handlers:

```csharp
_logger.LogInformation(
    "Processing {EventType} for order {OrderId}",
    notification.GetType().Name,
    notification.AggregateId);
```

### Monitor these metrics:
- Event publishing latency
- Event handler execution time
- Event publishing errors
- Event handler errors

### Create alerts for:
- Event publishing failures
- Event handler timeouts
- Event handler exceptions

---

## Rollback Plan

If issues arise:

1. **Revert OrderRepository** - Keep old query methods
2. **Disable event publishing** - Comment out event publish code
3. **Revert handlers** - Remove new event handlers
4. **Database rollback** - Restore from backup if needed

---

## Success Criteria

✅ All handlers inject OrderDomainService  
✅ All state changes publish events  
✅ Event handlers process events  
✅ Specifications used for queries  
✅ OrderMapper used consistently  
✅ Tests verify domain logic  
✅ Logging shows events flowing  
✅ Performance within SLA  

---

## Support

For issues:
1. Check **RICH_DOMAIN_QUICK_REFERENCE.md**
2. Review **RICH_DOMAIN_EXAMPLES.md** for code samples
3. Check implementation details in **RICH_DOMAIN_IMPLEMENTATION.md**
4. See specific file changes in **RICH_DOMAIN_CHANGES.md**

Good luck! 🚀
