using MediatR;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Application.Mappers;

namespace OrderService.Application.Commands.Handlers;

/// <summary>
/// Handler for confirming orders.
/// Uses Unit of Work to coordinate transactional operations.
/// Validates order state and reserves stock.
/// </summary>
public class ConfirmOrderCommandHandler : IRequestHandler<ConfirmOrderCommand, OrderResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmOrderCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderResponse> Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(request.OrderId, cancellationToken);
            if (order == null)
                throw new InvalidOperationException($"Order {request.OrderId} not found");

            // Validate business rules
            if (!order.CanBeConfirmed())
                throw new InvalidOperationException($"Cannot confirm order in {order.Status.Name} status. Only Placed orders can be confirmed.");

            var productIds = order.Items.Select(i => i.ProductId).ToList();
            var products = await _unitOfWork.Products.GetByIdsForUpdateAsync(productIds, cancellationToken);

            foreach (var item in order.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                product.ReserveStock(item.Quantity);
            }

            // Confirm the order
            order.Confirm();
            
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
