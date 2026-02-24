using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Services;

/// <summary>
/// Domain service that handles complex operations involving multiple aggregates or domain concepts.
/// This service encapsulates cross-cutting business logic that doesn't belong to a single entity.
/// </summary>
public class OrderDomainService
{
    /// <summary>
    /// Validates if an order can be confirmed based on business rules.
    /// Can be extended to check inventory, payment, etc.
    /// </summary>
    public bool CanConfirmOrder(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        return order.Status.CanTransitionToConfirmed();
    }

    /// <summary>
    /// Validates if an order can be canceled based on business rules.
    /// Can be extended with additional business rule checks.
    /// </summary>
    public bool CanCancelOrder(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        return order.Status.CanTransitionToCanceled();
    }

    /// <summary>
    /// Validates if an order can be modified (e.g., items added/removed).
    /// Orders in terminal states cannot be modified.
    /// </summary>
    public bool CanModifyOrder(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        return !order.Status.IsTerminal();
    }

    /// <summary>
    /// Determines if an order has sufficient stock for all items.
    /// This is a placeholder for integration with inventory service.
    /// </summary>
    public bool HasSufficientStock(Order order, Dictionary<Guid, int> availableStock)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));
        if (availableStock == null)
            throw new ArgumentNullException(nameof(availableStock));

        foreach (var item in order.Items)
        {
            if (!availableStock.ContainsKey(item.ProductId) || availableStock[item.ProductId] < item.Quantity)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Calculates whether the order total meets minimum requirements.
    /// Can be extended with customer-specific rules (e.g., minimum order value).
    /// </summary>
    public bool MeetsMinimumOrderValue(Order order, decimal minimumValue = 0)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        return order.Total >= minimumValue;
    }
}
