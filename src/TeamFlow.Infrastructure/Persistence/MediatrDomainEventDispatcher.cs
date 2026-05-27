using MediatR;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Infrastructure.Persistence;

/// <summary>
/// Bridges domain events into MediatR. Domain events that also implement <see cref="INotification"/>
/// (via the wrapper below) will be published to all registered handlers.
/// </summary>
internal sealed class MediatrDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;
    public MediatrDomainEventDispatcher(IPublisher publisher) => _publisher = publisher;

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var domainEvent in events)
            await _publisher.Publish(new DomainEventNotification(domainEvent), ct);
    }
}

/// <summary>MediatR notification wrapper for any <see cref="IDomainEvent"/>.</summary>
public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;
