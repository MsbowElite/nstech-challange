# Order Service API - Technical Decisions

## Architecture Overview

This project implements a **Clean Architecture** approach with clear separation of concerns across four layers:

### 1. Domain Layer
- **Pure business logic** with no external dependencies
- **Entities**: Order, Product
- **Value Objects**: OrderItem
- **Enums**: OrderStatus (Draft, Placed, Confirmed, Canceled)
- **Domain Exceptions**: Custom exceptions for business rule violations

**Key Decisions:**
- OrderItem is a value object owned by Order (DDD approach)
- Order entity enforces all business rules (e.g., status transitions)
- Stock is managed at the Product level with Reserve/Release methods

### 2. Application Layer
- **CQRS Pattern** using MediatR
- **Commands**: CreateOrder, ConfirmOrder, CancelOrder
- **Queries**: GetOrderById, GetOrders (with pagination and filters)
- **DTOs**: Request/Response objects for API communication

**Key Decisions:**
- Used MediatR for separation of command/query handling
- Idempotency implemented at the command handler level
- Stock validation happens during order creation (fail-fast)
- Stock reservation happens during order confirmation

### 3. Infrastructure Layer
- **EF Core 8.0** with PostgreSQL (Npgsql)
- **Repository Pattern** for data access
- **Database Context** with explicit entity configurations
- **Migrations** for database schema management

**Key Decisions:**
- OrderItem stored as owned entity type (no separate table with public key)
- Idempotency records stored in separate table with key as primary key
- Indexes added on Order fields commonly used for filtering (CustomerId, Status, CreatedAt)

### 4. API Layer
- **ASP.NET Core 8.0 Web API**
- **JWT Authentication** for securing endpoints
- **Swagger/OpenAPI** for documentation
- **Dependency Injection** for all services

**Key Decisions:**
- All endpoints except /auth/token require authentication
- Idempotency-Key header supported (optional) for confirm/cancel operations
- If no idempotency key provided, one is auto-generated (prevents accidental duplicates)

## Business Logic Decisions

### Order Creation
1. Validates all products exist
2. Checks stock availability BEFORE creating order
3. Creates order in "Placed" status
4. Does NOT reserve stock (only happens on confirmation)

### Order Confirmation (Idempotent)
1. Checks idempotency key to prevent duplicate processing
2. Validates order is in "Placed" status
3. Reserves stock from products
4. Transitions order to "Confirmed"
5. Records idempotency key

### Order Cancellation (Idempotent)
1. Checks idempotency key
2. Releases stock if order was "Confirmed"
3. Transitions order to "Canceled"
4. Records idempotency key

### Why This Stock Management Approach?

- **Two-phase approach**: Check availability at creation, reserve at confirmation
- **Benefits**:
  - Prevents overselling
  - Allows orders to be placed quickly without locking stock
  - Stock only locked when payment/confirmation happens
- **Trade-offs**:
  - Order might fail at confirmation if stock depleted
  - Acceptable for this use case (better UX during order creation)

## Security

### JWT Authentication
- Simple username/password authentication (for demo)
- In production: integrate with identity provider (Azure AD, Auth0, etc.)
- Tokens expire after 1 hour
- Secret key stored in configuration (should use Azure Key Vault in production)

### Authorization
- All endpoints require authentication except /auth/token
- In production: add role-based authorization

## Performance Considerations

1. **Database Indexes**: Added on frequently queried fields
2. **Async/Await**: Used throughout for non-blocking I/O
3. **AsNoTracking**: Used for read-only queries
4. **Pagination**: Required for list endpoints to prevent large result sets

## Testing Strategy

1. **Domain Layer Tests**: Pure unit tests for business logic
2. **Application Layer Tests**: Would test command/query handlers with mocked repositories
3. **Integration Tests**: Would test full API endpoints with test database

## Docker & DevOps

1. **Multi-stage Dockerfile**: Optimizes image size
2. **Docker Compose**: Orchestrates API + PostgreSQL
3. **Health Checks**: Ensures database is ready before API starts
4. **Auto-migrations**: Database schema applied on startup

## Future Enhancements

If this were a production system, I would add:

1. **Better Authentication**: Integration with OAuth2/OIDC provider
2. **Logging & Monitoring**: Structured logging, APM (Application Insights)
3. **Caching**: Redis for frequently accessed data
4. **Events**: Domain events for order state changes
5. **Message Queue**: Async processing for long-running operations
6. **API Versioning**: Support multiple API versions
7. **Rate Limiting**: Protect against abuse
8. **Circuit Breaker**: Resilience for external dependencies
9. **Background Jobs**: Cleanup old idempotency records
10. **Audit Trail**: Track all changes to orders

## Trade-offs Made

1. **Simplified Auth**: JWT without proper user management (acceptable for technical test)
2. **No Distributed Transactions**: Stock reservation could fail after order confirmation
3. **In-memory Idempotency**: Uses database table (could use Redis for better performance)
4. **No Event Sourcing**: Could track full order history with events
5. **Single Currency**: Assumes all prices in one currency (could extend to multi-currency)

## Why These Technologies?

- **.NET 8**: Latest LTS version with best performance
- **PostgreSQL**: Robust, open-source, excellent for transactional workloads
- **EF Core**: ORM with good performance and migration support
- **MediatR**: Keeps code organized with CQRS pattern
- **JWT**: Industry standard for API authentication
- **Docker**: Easy deployment and environment consistency