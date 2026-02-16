using MediatR;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

namespace OrderService.Application.Commands.Handlers;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    public CreateOrderCommandHandler(IOrderRepository orderRepository, IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    public async Task<OrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        // Validate products exist and have sufficient stock
        var productIds = req.Items.Select(i => i.ProductId).ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);

        if (products.Count != productIds.Distinct().Count())
        {
            var missing = productIds.Except(products.Select(p => p.Id)).ToList();
            throw new InvalidOperationException($"Products not found: {string.Join(", ", missing)}");
        }

        var orderItems = new List<OrderItem>();
        foreach (var item in req.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            
            if (product.AvailableQuantity < item.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for product {product.Name}. Available: {product.AvailableQuantity}, Requested: {item.Quantity}");
            }

            orderItems.Add(new OrderItem(product.Id, product.UnitPrice, item.Quantity));
        }

        var order = new Order(req.CustomerId, req.Currency, orderItems);
        await _orderRepository.AddAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(order);
    }

    private OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.Currency,
            order.Total,
            order.Items.Select(i => new OrderItemResponse(i.ProductId, i.UnitPrice, i.Quantity, i.Subtotal)).ToList(),
            order.CreatedAt,
            order.UpdatedAt
        );
    }
}
