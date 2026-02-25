using MediatR;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Application.Mappers;
using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

namespace OrderService.Application.Commands.Handlers;

/// <summary>
/// Handler for creating new orders.
/// Uses separate repositories and Unit of Work for save coordination.
/// Validates products and creates the order aggregate.
/// </summary>
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
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

        // Create the order aggregate
        var order = new Order(req.CustomerId, req.Currency, orderItems);

        // Persist the order through repository
        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OrderMapper.MapToResponse(order);
    }
}
