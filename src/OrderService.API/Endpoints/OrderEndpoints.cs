using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Queries;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OrderService.API.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/orders")
            .RequireAuthorization();

        orders.MapPost("/", CreateOrder)
            .WithName("CreateOrder")
            .WithOpenApi();

        orders.MapPost("/{id}/confirm", ConfirmOrder)
            .WithName("ConfirmOrder")
            .WithOpenApi();

        orders.MapPost("/{id}/cancel", CancelOrder)
            .WithName("CancelOrder")
            .WithOpenApi();

        orders.MapGet("/{id}", GetOrderById)
            .WithName("GetOrderById")
            .WithOpenApi();

        orders.MapGet("/", GetOrders)
            .WithName("GetOrders")
            .WithOpenApi();
    }

    private static async Task<IResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateOrderCommand(request);
            var result = await mediator.Send(command, cancellationToken);
            return Results.Created($"/orders/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ConfirmOrder(
        Guid id,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault()
                ?? Guid.NewGuid().ToString();
            var command = new ConfirmOrderCommand(id, idempotencyKey);
            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> CancelOrder(
        Guid id,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault()
                ?? Guid.NewGuid().ToString();
            var command = new CancelOrderCommand(id, idempotencyKey);
            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetOrderById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetOrderByIdQuery(id);
        var result = await mediator.Send(query, cancellationToken);

        if (result == null)
            return Results.NotFound(new { error = $"Order {id} not found" });

        return Results.Ok(result);
    }

    private static async Task<IResult> GetOrders(
        [AsParameters] OrderListQuery queryParams,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetOrdersQuery(queryParams);
        var result = await mediator.Send(query, cancellationToken);
        return Results.Ok(result);
    }
}
