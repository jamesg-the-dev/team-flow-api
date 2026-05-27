using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Infrastructure.Persistence.Interceptors;

/// <summary>Stamps Created/Updated timestamps + user ids on auditable aggregates.</summary>
public sealed class AuditingInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AuditingInterceptor(ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _clock = clock;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default
    )
    {
        if (eventData.Context is not null)
            Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void Stamp(DbContext ctx)
    {
        var now = _clock.UtcNow;
        var userId = _currentUser.UserId;

        foreach (EntityEntry<IAuditable> e in ctx.ChangeTracker.Entries<IAuditable>())
        {
            switch (e.State)
            {
                case EntityState.Added:
                    e.Entity.SetCreated(now, userId);
                    break;
                case EntityState.Modified:
                    e.Entity.SetUpdated(now, userId);
                    break;
            }
        }
    }
}
