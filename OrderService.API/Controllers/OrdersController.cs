using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Queries;

namespace OrderService.API.Controllers;

[ApiController]
[Route("orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateOrderCommand(request);
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetOrderById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/confirm")]
    public async Task<ActionResult<OrderResponse>> ConfirmOrder(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            // Generate idempotency key from request headers or use a default
            var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault() ?? Guid.NewGuid().ToString();
            var command = new ConfirmOrderCommand(id, idempotencyKey);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<OrderResponse>> CancelOrder(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            // Generate idempotency key from request headers or use a default
            var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault() ?? Guid.NewGuid().ToString();
            var command = new CancelOrderCommand(id, idempotencyKey);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponse>> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetOrderByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        
        if (result == null)
            return NotFound(new { error = $"Order {id} not found" });

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderResponse>>> GetOrders(
        [FromQuery] Guid? customerId,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetOrdersQuery(new OrderListQuery(customerId, status, from, to, page, pageSize));
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
