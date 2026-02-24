namespace OrderService.Domain.Interfaces;

using OrderService.Domain.Events;

/// <summary>
/// Interface for aggregate roots that raise domain events.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Gets the collection of uncommitted domain events.
    /// </summary>
    IReadOnlyCollection<DomainEvent> GetUncommittedEvents();

    /// <summary>
    /// Marks all domain events as committed/processed.
    /// </summary>
    void ClearUncommittedEvents();
}
