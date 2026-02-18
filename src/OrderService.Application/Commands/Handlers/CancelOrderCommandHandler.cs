using MediatR;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;

namespace OrderService.Application.Commands.Handlers;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    public async Task<OrderResponse> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdForUpdateAsync(request.OrderId, cancellationToken);
        if (order == null)
            throw new InvalidOperationException($"Order {request.OrderId} not found");

        if (!order.CanBeCanceled())
            throw new InvalidOperationException($"Cannot cancel order in {order.Status} status. Only Placed or Confirmed orders can be canceled.");

        // Only release stock if order was confirmed
        if (order.IsConfirmed())
        {
            // Release stock
            var productIds = order.Items.Select(i => i.ProductId).ToList();
            var products = await _productRepository.GetByIdsForUpdateAsync(productIds, cancellationToken);

            foreach (var item in order.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                product.ReleaseStock(item.Quantity);
            }

            await _productRepository.SaveChangesAsync(cancellationToken);
        }

        order.Cancel();
        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

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
