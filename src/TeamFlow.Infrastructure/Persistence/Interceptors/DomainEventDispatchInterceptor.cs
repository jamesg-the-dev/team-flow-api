using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Collects domain events from tracked aggregates and dispatches them AFTER a successful save.
/// Keeps the domain free of MediatR dependencies.
/// </summary>
public sealed class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private readonly IDomainEventDispatcher _dispatcher;
    public DomainEventDispatchInterceptor(IDomainEventDispatcher dispatcher) => _dispatcher = dispatcher;

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        if (eventData.Context is null) return result;

        var aggregates = eventData.Context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        foreach (var a in aggregates) a.ClearDomainEvents();

        if (events.Count > 0)
            await _dispatcher.DispatchAsync(events, ct);

        return result;
    }
}
