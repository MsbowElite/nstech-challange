using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Application.Mappers;
using OrderService.Application.Queries;

namespace OrderService.Application.Queries.Handlers;

/// <summary>
/// Handler for retrieving an order by ID.
/// Uses no-tracking queries for optimal read performance.
/// </summary>
public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderResponse?>
{
    private readonly IOrderDbContextReadOnly _context;

    public GetOrderByIdQueryHandler(IOrderDbContextReadOnly context)
    {
        _context = context;
    }

    public async Task<OrderResponse?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        // Query with AsNoTracking for optimal performance
        var order = await _context.Orders
            .Where(o => o.Id == request.OrderId)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (order == null)
            return null;

        return OrderMapper.MapToResponse(order);
    }
}

/// <summary>
/// Handler for retrieving multiple orders with filtering and pagination.
/// Uses no-tracking queries for optimal read performance.
/// </summary>
public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedResult<OrderResponse>>
{
    private readonly IOrderDbContextReadOnly _context;

    public GetOrdersQueryHandler(IOrderDbContextReadOnly context)
    {
        _context = context;
    }

    public async Task<PagedResult<OrderResponse>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = request.Query;
        
        // Build query with AsNoTracking for optimal performance
        var baseQuery = _context.Orders;

        // Apply filters
        if (query.CustomerId.HasValue)
        {
            baseQuery = baseQuery.Where(o => o.CustomerId == query.CustomerId.Value);
        }

        if (!string.IsNullOrEmpty(query.Status))
        {
            baseQuery = baseQuery.Where(o => o.Status.Name == query.Status);
        }

        if (query.From.HasValue)
        {
            baseQuery = baseQuery.Where(o => o.CreatedAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            baseQuery = baseQuery.Where(o => o.CreatedAt <= query.To.Value);
        }

        // Get total count
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        // Apply pagination
        var orders = await baseQuery
            .OrderByDescending(o => o.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var orderResponses = orders.Select(OrderMapper.MapToResponse).ToList();

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
