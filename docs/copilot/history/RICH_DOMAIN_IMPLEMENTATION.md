# Rich Domain Implementation Guide

This document outlines the Rich Domain Design patterns applied to the Order Service project.

## Overview

A Rich Domain Model emphasizes business logic and rules within the domain layer, rather than having anemic entities with logic scattered across application services. The Order Service now follows these key patterns:

---

## 1. Aggregate Root Pattern

### Order Entity (`src/OrderService.Domain/Entities/Order.cs`)

The `Order` class is now an **Aggregate Root** that:
- Implements `IAggregateRoot` interface
- Encapsulates all business logic for order management
- Maintains consistency of related objects (OrderItems)
- Raises domain events to notify external systems
- Provides only necessary public methods for state transitions

**Key Methods:**
- `Confirm()` - Transitions order from Placed to Confirmed
- `Cancel(reason)` - Transitions order from Placed/Confirmed to Canceled
- `GetUncommittedEvents()` - Retrieves raised domain events
- `ClearUncommittedEvents()` - Clears events after processing

---

## 2. Domain Events

### Event System (`src/OrderService.Domain/Events/`)

Domain events represent meaningful business occurrences:

**Base Class: `DomainEvent`**
- All events inherit from this base class
- Contains `AggregateId` and `OccurredAt` timestamp
- Used for event sourcing and notifications

**Event Types:**

1. **OrderCreatedEvent** - Raised when an order is created
   - Contains: CustomerId, Currency, Total, CreatedAt
   - Notifies inventory and notification services

2. **OrderConfirmedEvent** - Raised when an order is confirmed
   - Contains: CustomerId, ConfirmedAt
   - Triggers stock reservation

3. **OrderCanceledEvent** - Raised when an order is canceled
   - Contains: CustomerId, Reason, CanceledAt
   - Triggers stock release and notifications

**Usage in Handlers:**
```csharp
// Create aggregate (raises events)
var order = new Order(customerId, currency, items);

// Retrieve raised events
var events = order.GetUncommittedEvents();

// Publish to external handlers
foreach (var evt in events)
{
    await _publisher.Publish(evt, cancellationToken);
}

// Clear after processing
order.ClearUncommittedEvents();
```

---

## 3. Rich Value Objects

### OrderStatus (`src/OrderService.Domain/ValueObjects/OrderStatus.cs`)

Replaced simple enum with a rich enumeration that encapsulates behavior:

**Characteristics:**
- Immutable value object
- Contains status values, names, and descriptions
- **Behavior Methods:**
  - `CanTransitionToConfirmed()` - Validates state transitions
  - `CanTransitionToCanceled()` - Validates cancellation rules
  - `IsTerminal()` - Checks if order is in final state
- Factory methods: `FromValue()`, `FromName()`, `GetAll()`

**Status Values:**
- `Draft (0)` - Initial state
- `Placed (1)` - Order placed by customer
- `Confirmed (2)` - Order confirmed and stock reserved
- `Canceled (3)` - Order canceled (terminal state)

**Before:**
```csharp
if (order.Status != OrderStatus.Placed)
    throw new InvalidOperationException(...);
```

**After:**
```csharp
if (!order.Status.CanTransitionToConfirmed())
    throw new InvalidOperationException(...);
```

---

## 4. Domain Services

### OrderDomainService (`src/OrderService.Domain/Services/OrderDomainService.cs`)

Encapsulates cross-aggregate business logic that doesn't belong to a single entity:

**Methods:**
- `CanConfirmOrder(order)` - Complex validation before confirmation
- `CanCancelOrder(order)` - Complex validation before cancellation
- `CanModifyOrder(order)` - Checks if order can be modified
- `HasSufficientStock(order, availableStock)` - Inventory validation
- `MeetsMinimumOrderValue(order, minimumValue)` - Business rule validation

**Usage:**
```csharp
if (!_domainService.CanConfirmOrder(order))
    throw new InvalidOperationException("Cannot confirm order");
```

---

## 5. Specification Pattern

### Location: `src/OrderService.Domain/Specifications/`

Encapsulates query logic as reusable, testable objects:

**Base Class: `Specification<T>`**
- Provides common query building methods
- Supports: filtering, ordering, pagination, includes

**Concrete Specifications:**

1. **OrderByIdSpecification** - Query a single order by ID
2. **OrderByCustomerIdSpecification** - Query orders for a customer
3. **OrdersByStatusSpecification** - Query orders by status
4. **OrdersInDateRangeSpecification** - Query orders within date range

**Advantages:**
- Encapsulates query logic
- Reusable across handlers
- Easily testable
- Clear intent

**Usage:**
```csharp
var spec = new OrderByIdSpecification(orderId);
var order = await _orderRepository.GetBySpecificationAsync(spec, cancellationToken);
```

---

## 6. Aggregate Root Interface

### IAggregateRoot (`src/OrderService.Domain/Interfaces/IAggregateRoot.cs`)

Provides contract for all aggregate roots:

```csharp
public interface IAggregateRoot
{
    IReadOnlyCollection<DomainEvent> GetUncommittedEvents();
    void ClearUncommittedEvents();
}
```

Benefits:
- Standardized event handling
- Type-safe aggregate identification
- Supports event sourcing patterns

---

## 7. Mapper Layer

### OrderMapper (`src/OrderService.Application/Mappers/OrderMapper.cs`)

Centralizes DTO mapping logic:

**Responsibilities:**
- Converts Order entities to OrderResponse DTOs
- Handles null validation
- Ensures consistent mapping across handlers
- Uses rich value object names (e.g., `order.Status.Name`)

**Before:** Inline mapping in each handler
**After:** Centralized, reusable mapper

---

## 8. Updated Application Layer

### Command Handlers

All handlers now:
1. Use **domain service** for business rule validation
2. **Raise domain events** within aggregates
3. **Publish events** to external handlers
4. Use **mapper** for consistent DTO creation

**Handler Pattern:**
```csharp
// 1. Load aggregate
var order = await _orderRepository.GetByIdForUpdateAsync(id, cancellationToken);

// 2. Validate using domain service
if (!_domainService.CanConfirmOrder(order))
    throw new InvalidOperationException(...);

// 3. Execute domain logic
order.Confirm(); // Raises OrderConfirmedEvent

// 4. Persist changes
await _orderRepository.SaveChangesAsync(cancellationToken);

// 5. Publish domain events
var events = order.GetUncommittedEvents();
foreach (var evt in events)
    await _publisher.Publish(evt, cancellationToken);
order.ClearUncommittedEvents();

// 6. Return mapped response
return OrderMapper.MapToResponse(order);
```

### Query Handlers

Use **specifications** for query logic:

```csharp
var spec = new OrderByIdSpecification(request.OrderId);
var order = await _orderRepository.GetBySpecificationAsync(spec, cancellationToken);
return OrderMapper.MapToResponse(order);
```

---

## 9. Repository Interface Updates

### IOrderRepository (`src/OrderService.Application/Interfaces/IRepositories.cs`)

Added specification support:

```csharp
Task<Order?> GetBySpecificationAsync(
    Specification<Order> spec, 
    CancellationToken cancellationToken = default);
```

---

## Benefits of Rich Domain Implementation

1. **Business Logic Centralization** - Rules live in the domain, not scattered
2. **Type Safety** - Rich value objects replace primitive types
3. **Encapsulation** - Aggregate roots maintain invariants
4. **Testability** - Domain logic tests don't need databases
5. **Event Sourcing Ready** - Domain events enable event sourcing
6. **Maintainability** - Clear separation of concerns
7. **Reusability** - Specifications and services are reusable
8. **Intent Clarity** - Code expresses business intent

---

## Domain Model Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                        Domain Layer                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────────────────────────────────────┐      │
│  │           Order (Aggregate Root)                 │      │
│  │  - Manages order lifecycle                       │      │
│  │  - Validates state transitions                   │      │
│  │  - Raises domain events                          │      │
│  └──────────────────────────────────────────────────┘      │
│         │              │              │                     │
│         ▼              ▼              ▼                     │
│    ┌─────────┐   ┌────────────┐  ┌──────────────┐         │
│    │OrderItem│   │OrderStatus │  │OrderDomainSvc│         │
│    │(VO)     │   │(Rich VO)   │  │              │         │
│    └─────────┘   └────────────┘  └──────────────┘         │
│                                                             │
│  ┌──────────────────────────────────────────────────┐      │
│  │              Domain Events                       │      │
│  │  - OrderCreatedEvent                            │      │
│  │  - OrderConfirmedEvent                          │      │
│  │  - OrderCanceledEvent                           │      │
│  └──────────────────────────────────────────────────┘      │
│                                                             │
│  ┌──────────────────────────────────────────────────┐      │
│  │            Specifications                        │      │
│  │  - OrderByIdSpecification                        │      │
│  │  - OrderByCustomerIdSpecification                │      │
│  │  - OrdersByStatusSpecification                   │      │
│  └──────────────────────────────────────────────────┘      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
        │                   │                   │
        ▼                   ▼                   ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  Repository  │  │   Mapper     │  │ Event Pub    │
│  (Interface) │  │              │  │              │
└──────────────┘  └──────────────┘  └──────────────┘
```

---

## Next Steps for Enhancement

1. **Event Sourcing** - Store events as the single source of truth
2. **CQRS** - Separate read and write models
3. **Saga Pattern** - Manage distributed transactions across services
4. **Business Rules Engine** - Externalize complex business rules
5. **Domain Event Subscriptions** - Implement reactive handlers for events
6. **Bounded Contexts** - Expand to other subdomains (Inventory, Payment, etc.)

---

## References

- Domain-Driven Design (Eric Evans)
- Implementing Domain-Driven Design (Vaughn Vernon)
- Microsoft Architecture Patterns
