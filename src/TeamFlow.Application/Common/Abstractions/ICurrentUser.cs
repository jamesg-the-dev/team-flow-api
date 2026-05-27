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

    /// <summary>Email claim from the Supabase access token (may be null for some OAuth flows).</summary>
    string? Email { get; }
    bool EmailVerified { get; }

    /// <summary>Convenience reads over <c>user_metadata</c> populated by Supabase.</summary>
    string? FullName { get; }
    string? AvatarUrl { get; }
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
    Task DispatchAsync(
        IEnumerable<TeamFlow.Domain.SeedWork.IDomainEvent> events,
        CancellationToken ct = default
    );
}
