using MediatR;
using OrderService.Application.DTOs;

namespace OrderService.Application.Queries;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderResponse?>;

public record GetOrdersQuery(OrderListQuery Query) : IRequest<PagedResult<OrderResponse>>;
