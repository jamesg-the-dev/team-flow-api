namespace TeamFlow.Application.Common.Abstractions;

/// <summary>
/// Resolves the authenticated Supabase user from the current request. Implemented in the API layer
/// by reading the JWT 'sub' claim (uuid) populated by Supabase Auth.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    Guid RequireUserId();
}

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// Dispatches aggregate-emitted domain events after a successful unit of work.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<TeamFlow.Domain.SeedWork.IDomainEvent> events, CancellationToken ct = default);
}
