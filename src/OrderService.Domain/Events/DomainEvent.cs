using MediatR;

namespace OrderService.Domain.Events;

/// <summary>
/// Base class for all domain events in the Order Service.
/// Domain events represent meaningful things that happen within the domain
/// and can be used to notify other parts of the system of state changes.
/// </summary>
public abstract class DomainEvent : INotification
{
    public Guid AggregateId { get; protected set; }
    public DateTime OccurredAt { get; protected set; } = DateTime.UtcNow;

    protected DomainEvent(Guid aggregateId)
    {
        AggregateId = aggregateId;
    }
}
