namespace OrderService.Application.Interfaces;

/// <summary>
/// Unit of Work pattern implementation.
/// Coordinates multiple repositories and manages a single transaction.
/// Provides a single point of access to all repositories and transaction management.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// Gets the Order repository.
    /// </summary>
    IOrderRepository Orders { get; }

    /// <summary>
    /// Gets the Product repository.
    /// </summary>
    IProductRepository Products { get; }

    /// <summary>
    /// Commits all changes made to repositories in the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
