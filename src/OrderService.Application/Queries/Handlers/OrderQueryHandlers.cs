using MediatR;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Application.Queries;

namespace OrderService.Application.Queries.Handlers;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderResponse?>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderResponse?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        
        if (order == null)
            return null;

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

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedResult<OrderResponse>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<PagedResult<OrderResponse>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = request.Query;
        var (orders, totalCount) = await _orderRepository.GetPagedAsync(
            query.CustomerId,
            query.Status,
            query.From,
            query.To,
            query.Page,
            query.PageSize,
            cancellationToken);

        var orderResponses = orders.Select(order => new OrderResponse(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.Currency,
            order.Total,
            order.Items.Select(i => new OrderItemResponse(i.ProductId, i.UnitPrice, i.Quantity, i.Subtotal)).ToList(),
            order.CreatedAt,
            order.UpdatedAt
        )).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return new PagedResult<OrderResponse>(
            orderResponses,
            query.Page,
            query.PageSize,
            totalCount,
            totalPages
        );
    }
}
