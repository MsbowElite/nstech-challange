using OrderService.Application.DTOs;
using OrderService.Domain.Entities;

namespace OrderService.Application.Mappers;

/// <summary>
/// Mapper for converting Order entities to response DTOs.
/// Centralizes the mapping logic to ensure consistency across the application.
/// </summary>
public static class OrderMapper
{
    public static OrderResponse MapToResponse(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        return new OrderResponse(
            order.Id,
            order.CustomerId,
            order.Status.Name,
            order.Currency,
            order.Total,
            order.Items.Select(i => new OrderItemResponse(i.ProductId, i.UnitPrice, i.Quantity, i.Subtotal)).ToList(),
            order.CreatedAt,
            order.UpdatedAt
        );
    }
}
