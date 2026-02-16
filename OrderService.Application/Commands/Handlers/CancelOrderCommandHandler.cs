using MediatR;
using OrderService.Application.Commands;
using OrderService.Application.Common;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;

namespace OrderService.Application.Commands.Handlers;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IIdempotencyRepository _idempotencyRepository;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IIdempotencyRepository idempotencyRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _idempotencyRepository = idempotencyRepository;
    }

    public async Task<OrderResponse> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        // Check idempotency
        var idempotencyKey = IdempotencyKeys.ForCancel(request.OrderId, request.IdempotencyKey);
        if (await _idempotencyRepository.ExistsAsync(idempotencyKey, cancellationToken))
        {
            // Already processed, return current state
            var existingOrder = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
            if (existingOrder == null)
                throw new InvalidOperationException($"Order {request.OrderId} not found");
            
            return MapToResponse(existingOrder);
        }

        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
            throw new InvalidOperationException($"Order {request.OrderId} not found");

        // Only release stock if order was confirmed
        if (order.IsConfirmed() && order.CanBeCanceled())
        {
            // Release stock
            var productIds = order.Items.Select(i => i.ProductId).ToList();
            var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);

            foreach (var item in order.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                product.ReleaseStock(item.Quantity);
            }

            await _productRepository.SaveChangesAsync(cancellationToken);
        }

        if (order.CanBeCanceled())
        {
            order.Cancel();
            await _orderRepository.UpdateAsync(order, cancellationToken);
            await _orderRepository.SaveChangesAsync(cancellationToken);
        }

        // Mark as processed for idempotency
        await _idempotencyRepository.AddAsync(idempotencyKey, cancellationToken);

        return MapToResponse(order);
    }

    private OrderResponse MapToResponse(Domain.Entities.Order order)
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
