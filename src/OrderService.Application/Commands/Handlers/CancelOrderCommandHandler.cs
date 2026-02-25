using MediatR;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Application.Mappers;

namespace OrderService.Application.Commands.Handlers;

/// <summary>
/// Handler for canceling orders.
/// Uses Unit of Work to coordinate transactional operations.
/// Validates order state and releases reserved stock.
/// </summary>
public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, OrderResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderResponse> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(request.OrderId, cancellationToken);
            if (order == null)
                throw new InvalidOperationException($"Order {request.OrderId} not found");

            // Validate business rules
            if (!order.CanBeCanceled())
                throw new InvalidOperationException($"Cannot cancel order in {order.Status.Name} status. Only Placed or Confirmed orders can be canceled.");

            // Only release stock if order was confirmed
            if (order.IsConfirmed())
            {
                // Release stock
                var productIds = order.Items.Select(i => i.ProductId).ToList();
                var products = await _unitOfWork.Products.GetByIdsForUpdateAsync(productIds, cancellationToken);

                foreach (var item in order.Items)
                {
                    var product = products.First(p => p.Id == item.ProductId);
                    product.ReleaseStock(item.Quantity);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Cancel the order
            order.Cancel("Canceled by user request");
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return OrderMapper.MapToResponse(order);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
