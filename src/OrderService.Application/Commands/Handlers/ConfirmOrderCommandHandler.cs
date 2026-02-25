using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Application.Mappers;

namespace OrderService.Application.Commands.Handlers;

/// <summary>
/// Handler for confirming orders.
/// Uses separate repositories and Unit of Work for transaction coordination.
/// Validates order state and reserves stock.
/// Implements retry logic for concurrency conflicts.
/// </summary>
public class ConfirmOrderCommandHandler : IRequestHandler<ConfirmOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private const int MaxRetries = 3;

    public ConfirmOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderResponse> Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        int retryCount = 0;

        while (true)
        {
            try
            {
                var order = await _orderRepository.GetByIdForUpdateAsync(request.OrderId, cancellationToken);
                if (order == null)
                    throw new InvalidOperationException($"Order {request.OrderId} not found");

                order.Confirmable();

                var productIds = order.Items.Select(i => i.ProductId).ToList();
                var products = await _productRepository.GetByIdsForUpdateAsync(productIds, cancellationToken);

                foreach (var item in order.Items)
                {
                    var product = products.First(p => p.Id == item.ProductId);
                    product.ReserveStock(item.Quantity);
                }

                order.Confirm();

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return OrderMapper.MapToResponse(order);
            }
            catch (DbUpdateConcurrencyException) when (retryCount < MaxRetries)
            {
                retryCount++;
                // Wait a bit before retrying to reduce contention
                await Task.Delay(50 * retryCount, cancellationToken);
                // Context will reload entities on next iteration
            }
        }
    }
}
