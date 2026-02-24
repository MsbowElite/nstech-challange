using MediatR;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Application.Mappers;
using OrderService.Domain.Services;

namespace OrderService.Application.Commands.Handlers;

/// <summary>
/// Handler for canceling orders.
/// Uses Unit of Work to coordinate transactional operations.
/// Validates order state using domain service, releases reserved stock, and publishes domain events.
/// </summary>
public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, OrderResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly OrderDomainService _domainService;
    private readonly IPublisher _publisher;

    public CancelOrderCommandHandler(
        IUnitOfWork unitOfWork,
        OrderDomainService domainService,
        IPublisher publisher)
    {
        _unitOfWork = unitOfWork;
        _domainService = domainService;
        _publisher = publisher;
    }

    public async Task<OrderResponse> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(request.OrderId, cancellationToken);
            if (order == null)
                throw new InvalidOperationException($"Order {request.OrderId} not found");

            // Use domain service to validate business rules
            if (!_domainService.CanCancelOrder(order))
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

            // Cancel the order - this will raise domain events
            order.Cancel("Canceled by user request");
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            // Publish domain events
            var events = order.GetUncommittedEvents();
            foreach (var evt in events)
            {
                await _publisher.Publish(evt, cancellationToken);
            }
            order.ClearUncommittedEvents();

            return OrderMapper.MapToResponse(order);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
