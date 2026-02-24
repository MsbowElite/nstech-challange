# Rich Domain Architecture Diagrams

## 1. Domain Model Structure

```
┌─────────────────────────────────────────────────────────────────────┐
│                        ORDER AGGREGATE ROOT                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Order (IAggregateRoot)                                             │
│  ├─ Id: Guid                                                        │
│  ├─ CustomerId: Guid                                                │
│  ├─ Status: OrderStatus (Rich VO)                                   │
│  ├─ Currency: string                                                │
│  ├─ CreatedAt: DateTime                                             │
│  ├─ UpdatedAt: DateTime?                                            │
│  │                                                                  │
│  ├─ Items: List<OrderItem> (VO)                                     │
│  │  └─ OrderItem                                                    │
│  │     ├─ ProductId: Guid                                           │
│  │     ├─ UnitPrice: decimal                                        │
│  │     ├─ Quantity: int                                             │
│  │     └─ Subtotal: decimal (computed)                              │
│  │                                                                  │
│  ├─ _uncommittedEvents: List<DomainEvent>                          │
│  │                                                                  │
│  └─ Methods:                                                        │
│     ├─ Confirm() → Raises OrderConfirmedEvent                       │
│     ├─ Cancel(reason) → Raises OrderCanceledEvent                   │
│     ├─ GetUncommittedEvents() → IReadOnlyCollection<DomainEvent>   │
│     └─ ClearUncommittedEvents() → void                              │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
         │                       │                    │
         ▼                       ▼                    ▼
    ┌─────────────┐      ┌──────────────┐    ┌──────────────┐
    │ OrderStatus │      │  OrderItem   │    │  OrderDomain │
    │ (Rich VO)   │      │    (VO)      │    │  Service     │
    │             │      │              │    │              │
    │ • Placed    │      │ • ProductId  │    │ • CanConfirm │
    │ • Confirmed │      │ • UnitPrice  │    │ • CanCancel  │
    │ • Canceled  │      │ • Quantity   │    │ • HasStock   │
    │             │      │ • Subtotal   │    │ • CheckValue │
    │ Behavior:   │      └──────────────┘    └──────────────┘
    │ • CanTrans  │
    │ • IsTerminal│
    └─────────────┘
```

---

## 2. Domain Events Flow

```
┌──────────────────────────────────────────────────────────────────────┐
│                       DOMAIN EVENT LIFECYCLE                         │
└──────────────────────────────────────────────────────────────────────┘

    ┌──────────────┐
    │ Order        │
    │ Constructor  │
    │ or Method    │
    └──────┬───────┘
           │
           ▼
    ┌─────────────────────┐
    │ Create Event        │
    │ OrderCreatedEvent / │
    │ OrderConfirmedEvent │
    │ OrderCanceledEvent  │
    └──────┬──────────────┘
           │
           ▼
    ┌──────────────────────────────┐
    │ Add to                       │
    │ _uncommittedEvents           │
    │ (In-memory collection)       │
    └──────┬───────────────────────┘
           │
           ▼
    ┌──────────────────────────────┐
    │ Handler calls                │
    │ GetUncommittedEvents()       │
    └──────┬───────────────────────┘
           │
           ▼
    ┌──────────────────────────────┐
    │ For each event:              │
    │ _publisher.Publish(event)    │
    └──────┬───────────────────────┘
           │
           ▼
    ┌──────────────────────────────┐
    │ MediatR routes to:           │
    │ - NotificationHandlers       │
    │ - External subscribers       │
    └──────┬───────────────────────┘
           │
           ▼
    ┌──────────────────────────────┐
    │ Events processed:            │
    │ - Inventory service          │
    │ - Notification service       │
    │ - Audit service              │
    │ - Analytics service          │
    └──────┬───────────────────────┘
           │
           ▼
    ┌──────────────────────────────┐
    │ Call                         │
    │ ClearUncommittedEvents()     │
    │ (Reset for next operation)   │
    └──────────────────────────────┘
```

---

## 3. Request Flow - Confirming an Order

```
┌─────────────────────────────────────────────────────────────────────┐
│              HTTP REQUEST: POST /orders/{id}/confirm                │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │ ConfirmOrder     │
                    │ CommandHandler   │
                    └────────┬─────────┘
                             │
         ┌───────────────────┼───────────────────┐
         │                   │                   │
         ▼                   ▼                   ▼
    ┌────────────┐   ┌──────────────┐   ┌──────────────┐
    │  Load      │   │   Domain     │   │   Verify     │
    │  Order     │   │   Service    │   │   Can Change │
    │ from DB    │   │   Validation │   │              │
    │            │   │              │   │              │
    └────────┬───┘   └──────┬───────┘   └──────┬───────┘
             │              │                   │
             └──────────────┼───────────────────┘
                            │
                            ▼
                    ┌──────────────────┐
                    │ order.Confirm()  │
                    │                  │
                    │ • Validate state │
                    │ • Change status  │
                    │ • Raise event    │◄─── In Domain
                    │   OrderConfirmed │
                    │   Event          │
                    └────────┬─────────┘
                             │
                             ▼
                    ┌──────────────────┐
                    │ Save to Database │
                    └────────┬─────────┘
                             │
                             ▼
              ┌──────────────────────────────┐
              │ Get uncommitted events      │
              │ _publisher.Publish(events)  │
              └────────┬─────────────────────┘
                       │
        ┌──────────────┼──────────────┐
        │              │              │
        ▼              ▼              ▼
   ┌─────────┐  ┌──────────┐  ┌──────────┐
   │Inventory│  │Notification│ │ Analytics│
   │Handler  │  │  Handler   │ │ Handler  │
   │         │  │            │ │          │
   │Reserve  │  │Send Email  │ │Log Event │
   │Stock    │  │to Customer │ │to System │
   └─────────┘  └──────────┘  └──────────┘
        │              │              │
        └──────────────┼──────────────┘
                       │
                       ▼
              ┌──────────────────────┐
              │ Clear Events         │
              │ order.ClearUncommitted│
              │ Events()             │
              └────────┬─────────────┘
                       │
                       ▼
              ┌──────────────────────┐
              │ Return Response      │
              │ OrderResponse (DTO)  │
              │ using OrderMapper    │
              └────────┬─────────────┘
                       │
                       ▼
                 ┌──────────┐
                 │ 200 OK   │
                 │Response  │
                 └──────────┘
```

---

## 4. Specification Query Pattern

```
┌─────────────────────────────────────────────────────────────┐
│              SPECIFICATION PATTERN FLOW                    │
└─────────────────────────────────────────────────────────────┘

┌──────────────────────────────┐
│ Create Specification         │
│                              │
│ new OrderByCustomerId        │
│ Specification(customerId)    │
└────────────┬─────────────────┘
             │
             ▼
┌──────────────────────────────┐
│ Specification Object         │
│                              │
│ • Criteria: Func             │
│   (o => o.CustomerId == id)  │
│                              │
│ • OrderBy: Func              │
│   (o => o.CreatedAt)         │
│                              │
│ • Includes: List<Expr>       │
│                              │
│ • Skip, Take                 │
└────────────┬─────────────────┘
             │
             ▼
┌──────────────────────────────────┐
│ Repository.GetBySpec(...spec)   │
└────────────┬─────────────────────┘
             │
             ▼
┌──────────────────────────────────┐
│ Build EF Core Query:             │
│                                  │
│ query = DbSet.AsQueryable()     │
│                                  │
│ if (spec.Criteria != null)      │
│   query = query.Where(...)      │
│                                  │
│ foreach (include in Includes)   │
│   query = query.Include(...)    │
│                                  │
│ if (spec.OrderBy != null)       │
│   query = query.OrderBy(...)    │
│                                  │
│ if (spec.IsPaging)             │
│   query = query.Skip().Take()   │
└────────────┬─────────────────────┘
             │
             ▼
┌──────────────────────────────────┐
│ Execute Query                    │
│                                  │
│ FirstOrDefaultAsync()           │
│ ToListAsync()                   │
│ CountAsync()                    │
└────────────┬─────────────────────┘
             │
             ▼
┌──────────────────────────────────┐
│ Return Result                    │
│                                  │
│ Order | List<Order> | int       │
└──────────────────────────────────┘
```

---

## 5. Application Layer Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                         │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              COMMAND HANDLERS                       │   │
│  │                                                      │   │
│  │  CreateOrderCommandHandler                          │   │
│  │  ├─ Load products                                   │   │
│  │  ├─ Create Order aggregate                          │   │
│  │  ├─ Save to repository                              │   │
│  │  ├─ Get uncommitted events                          │   │
│  │  ├─ Publish events                                  │   │
│  │  └─ Return mapped DTO                               │   │
│  │                                                      │   │
│  │  ConfirmOrderCommandHandler                         │   │
│  │  ├─ Load order from repository                      │   │
│  │  ├─ Use domain service validation                   │   │
│  │  ├─ Call order.Confirm()                            │   │
│  │  ├─ Save to repository                              │   │
│  │  ├─ Publish events                                  │   │
│  │  └─ Return mapped DTO                               │   │
│  │                                                      │   │
│  │  CancelOrderCommandHandler                          │   │
│  │  ├─ Load order from repository                      │   │
│  │  ├─ Release stock if confirmed                      │   │
│  │  ├─ Call order.Cancel()                             │   │
│  │  ├─ Save to repository                              │   │
│  │  ├─ Publish events                                  │   │
│  │  └─ Return mapped DTO                               │   │
│  └─────────────────────────────────────────────────────┘   │
│                            │                                 │
│                            ▼                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              QUERY HANDLERS                         │   │
│  │                                                      │   │
│  │  GetOrderByIdQueryHandler                           │   │
│  │  ├─ Create OrderByIdSpecification                   │   │
│  │  ├─ Call repository.GetBySpec()                     │   │
│  │  └─ Map to OrderResponse                            │   │
│  │                                                      │   │
│  │  GetOrdersQueryHandler                              │   │
│  │  ├─ Build query with filters                        │   │
│  │  ├─ Execute paged query                             │   │
│  │  └─ Map each order to DTO                           │   │
│  └─────────────────────────────────────────────────────┘   │
│                            │                                 │
│                            ▼                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              MAPPER                                 │   │
│  │                                                      │   │
│  │  OrderMapper.MapToResponse(order)                   │   │
│  │  ├─ Create OrderResponse DTO                        │   │
│  │  ├─ Use status.Name (rich VO)                       │   │
│  │  ├─ Map all items                                   │   │
│  │  └─ Return DTO                                      │   │
│  └─────────────────────────────────────────────────────┘   │
│                            │                                 │
│                            ▼                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              REPOSITORY INTERFACE                   │   │
│  │                                                      │   │
│  │  • GetByIdAsync()                                   │   │
│  │  • GetBySpecificationAsync()  ◄─── NEW             │   │
│  │  • GetPagedAsync()                                  │   │
│  │  • AddAsync()                                       │   │
│  │  • UpdateAsync()                                    │   │
│  │  • SaveChangesAsync()                               │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## 6. Dependency Injection Graph

```
┌──────────────────────────────────────────────────────────┐
│                    DEPENDENCY INJECTION                   │
└──────────────────────────────────────────────────────────┘

                    IServiceCollection
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
    ┌─────────┐     ┌──────────────┐   ┌───────────────┐
    │ Domain  │     │ Application  │   │ Infrastructure│
    │ Service │     │  Handlers    │   │ Repositories │
    └────┬────┘     └──────┬───────┘   └───────┬───────┘
         │                 │                   │
         ▼                 ▼                   ▼
    ┌──────────────┐  ┌─────────────┐  ┌──────────────┐
    │OrderDomain   │  │Commands &   │  │ Repository   │
    │Service       │  │Queries      │  │Impl          │
    │              │  │             │  │              │
    │Uses:         │  │Uses:        │  │Implements:   │
    │• Validation  │  │• Domain Svc │  │• IOrderRepo  │
    │• Stock check │  │• Repository │  │• DbContext   │
    │• Rules       │  │• Publisher  │  │• Specs       │
    └──────────────┘  └─────────────┘  └──────────────┘
         │                 │                   │
         └─────────────────┼───────────────────┘
                           │
                           ▼
                    ┌─────────────────┐
                    │    MediatR      │
                    │ • Handlers      │
                    │ • Publishers    │
                    │ • Event Routes  │
                    └────────┬────────┘
                             │
                             ▼
                    ┌─────────────────┐
                    │ Event Handlers  │
                    │ (Subscribers)   │
                    │ • Inventory Svc │
                    │ • Notif Svc     │
                    │ • Audit Svc     │
                    └─────────────────┘
```

---

## 7. Status Transition State Machine

```
┌──────────────────────────────────────────────────────────┐
│            ORDER STATUS STATE MACHINE                   │
└──────────────────────────────────────────────────────────┘

    ┌─────────┐
    │ DRAFT   │
    │ (0)     │
    └────┬────┘
         │
         │ Manual transition
         │ (rarely used)
         ▼
    ┌───────────┐
    │ PLACED    │◄────────────────────────┐
    │ (1)       │                         │
    └──┬────┬───┘                         │
       │    │                             │
       │    └─ Can modify before confirm  │
       │                                  │
       ├────────────────────────┬─────────┘
       │                        │
       │ Confirm               │ Not confirmed
       │ (Final stock check)   │ (Auto release)
       │                        │
       ▼                        ▼
    ┌───────────┐          ┌──────────┐
    │ CONFIRMED │          │ CANCELED │
    │ (2)       │          │ (3)      │
    └──┬────────┘          └──────────┘
       │                        ▲
       │                        │
       └──────────────────────┬─┘
                              │
                    Refund stock
                    Release inventory
                    Notify customer

Rules:
✅ PLACED → CONFIRMED (only from Placed)
✅ PLACED → CANCELED (only from Placed or Confirmed)
✅ CONFIRMED → CANCELED (release stock)
❌ CONFIRMED → PLACED (not allowed)
❌ CANCELED → any (terminal state)
❌ DRAFT → CANCELED (not used in workflow)
```

---

## 8. Class Relationships

```
┌──────────────────────────────────────────────────────────┐
│              CLASS RELATIONSHIP DIAGRAM                  │
└──────────────────────────────────────────────────────────┘

           ┌────────────────────┐
           │  <<interface>>     │
           │  IAggregateRoot    │
           └──────────┬─────────┘
                      │
                      │ implements
                      │
                      ▼
           ┌────────────────────────┐
           │       Order            │
           ├────────────────────────┤
           │ - Id: Guid             │
           │ - CustomerId: Guid     │
           │ - Status: OrderStatus  │
           │ - Currency: string     │
           │ - _items: List<OItem>  │
           │ - _uncommitted: List   │
           │                        │
           │ + Confirm()            │
           │ + Cancel(reason)       │
           │ + GetUncommitted()     │
           │ + ClearUncommitted()   │
           └──────┬──────┬──────────┘
                  │      │ contains
        contains  │      │
                  │      ▼
                  │  ┌────────────────┐
                  │  │ OrderItem (VO) │
                  │  ├────────────────┤
                  │  │ - ProductId    │
                  │  │ - UnitPrice    │
                  │  │ - Quantity     │
                  │  │ - Subtotal     │
                  │  └────────────────┘
                  │
                  ▼
          ┌──────────────────┐
          │ OrderStatus (VO) │
          ├──────────────────┤
          │ - Value: int     │
          │ - Name: string   │
          │ - Descr: string  │
          │                  │
          │ + CanTrans..()   │
          │ + IsTerminal()   │
          └──────────────────┘

           ┌──────────────────────┐
           │  <<abstract>>        │
           │   DomainEvent        │
           ├──────────────────────┤
           │ - AggregateId: Guid  │
           │ - OccurredAt: DT     │
           └──────┬───────────────┘
                  │
        ┌─────────┼────────────┐
        │         │            │
        ▼         ▼            ▼
    ┌─────┐  ┌────────┐  ┌────────┐
    │Cre- │  │Conf-   │  │Canc-   │
    │ated │  │ irmed  │  │eled    │
    │     │  │        │  │        │
    │Ev   │  │Event   │  │Event   │
    └─────┘  └────────┘  └────────┘

        ┌──────────────────────────────┐
        │  <<interface>>               │
        │  IOrderRepository            │
        ├──────────────────────────────┤
        │ + GetByIdAsync()             │
        │ + GetBySpecAsync()  ◄────NEW │
        │ + GetPagedAsync()            │
        │ + AddAsync()                 │
        │ + UpdateAsync()              │
        │ + SaveChangesAsync()         │
        └───────────┬──────────────────┘
                    │ implements
                    ▼
        ┌──────────────────────────────┐
        │  OrderRepository             │
        └──────────────────────────────┘
```

---

## 9. Event Publishing & Subscription

```
┌──────────────────────────────────────────────────────────┐
│          DOMAIN EVENT PUB/SUB ARCHITECTURE               │
└──────────────────────────────────────────────────────────┘

    ┌───────────────────────┐
    │  Order Aggregate      │
    │  • Confirm()          │
    │  • Cancel()           │
    │  • Create()           │
    └──────────┬────────────┘
               │
               │ Raises events
               │ in methods
               │
               ▼
    ┌───────────────────────────────────┐
    │  Domain Events                    │
    │  • OrderCreatedEvent              │
    │  • OrderConfirmedEvent            │
    │  • OrderCanceledEvent             │
    └──────────┬────────────────────────┘
               │
               │ Stored in
               │ _uncommittedEvents
               │
               ▼
    ┌───────────────────────────────────┐
    │  Command Handler                  │
    │  • Gets events                    │
    │  • Publishes via IPublisher       │
    │  • Clears events                  │
    └──────────┬────────────────────────┘
               │
               │ Publishes to
               │ MediatR bus
               │
        ┌──────┴──────────────┐
        │                     │
        ▼                     ▼
    ┌────────────────────┐  ┌──────────────────┐
    │  MediatR routes   │  │ Multiple         │
    │  to event         │  │ subscribers can  │
    │  handlers         │  │ respond          │
    └────────┬──────────┘  └──────────────────┘
             │
    ┌────────┴────────┬──────────────┬──────────┐
    │                 │              │          │
    ▼                 ▼              ▼          ▼
 ┌──────┐         ┌─────────┐  ┌───────┐  ┌────────┐
 │Inv   │         │Email    │  │Analytics  │Audit │
 │Service│         │Service  │  │Service    │Log   │
 │      │         │         │  │          │      │
 │Reserve│         │Send     │  │Track     │Record│
 │Stock  │         │Confirm. │  │Events    │Event │
 │       │         │Email    │  │          │      │
 └──────┘         └─────────┘  └───────┘  └────────┘
```

---

This completes the comprehensive visual documentation of your rich domain architecture!
