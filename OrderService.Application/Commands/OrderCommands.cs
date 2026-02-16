using MediatR;
using OrderService.Application.DTOs;

namespace OrderService.Application.Commands;

public record CreateOrderCommand(CreateOrderRequest Request) : IRequest<OrderResponse>;

public record ConfirmOrderCommand(Guid OrderId, string IdempotencyKey) : IRequest<OrderResponse>;

public record CancelOrderCommand(Guid OrderId, string IdempotencyKey) : IRequest<OrderResponse>;
