namespace OrderService.Application.DTOs;

public record CreateOrderRequest(
    Guid CustomerId,
    string Currency,
    List<OrderItemRequest> Items
);

public record OrderItemRequest(
    Guid ProductId,
    int Quantity
);

public record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    string Currency,
    decimal Total,
    List<OrderItemResponse> Items,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record OrderItemResponse(
    Guid ProductId,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal
);

public record PagedResult<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);

public record OrderListQuery(
    Guid? CustomerId = null,
    string? Status = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 10
);
