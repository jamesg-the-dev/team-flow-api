namespace TeamFlow.Domain.SeedWork;

/// <summary>Marker for an aggregate root that emits domain events.</summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
