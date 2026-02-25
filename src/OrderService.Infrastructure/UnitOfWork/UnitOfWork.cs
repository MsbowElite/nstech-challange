using Microsoft.EntityFrameworkCore.Storage;
using OrderService.Application.Interfaces;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.UnitOfWork;

/// <summary>
/// Implementation of the Unit of Work pattern.
/// Manages the DbContext and coordinates save operations across repositories.
/// Provides explicit transaction management with commit/rollback semantics.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly OrderDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(OrderDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Saves all changes made to the DbContext.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("A concurrency conflict occurred. The order may have been modified by another process. Please retry the operation.");
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            throw new InvalidOperationException("An error occurred while updating the database. Please check your data and try again.", ex);
        }
    }

    /// <summary>
    /// Begins a new database transaction.
    /// Allows multiple repository operations to be committed or rolled back as a unit.
    /// </summary>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
            throw new InvalidOperationException("A transaction is already active. Complete it before starting a new one.");

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Commits the current transaction.
    /// Saves all changes and commits the transaction. Rolls back on error.
    /// </summary>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction is currently active.");

        try
        {
            await SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Rolls back the current transaction.
    /// Discards all uncommitted changes.
    /// </summary>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            return;

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
            _context.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Disposes the Unit of Work and its resources.
    /// Ensures transactions are properly cleaned up.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
        }
        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
