namespace TeamFlow.Application.Common.Realtime;

/// <summary>
/// Logical channel a realtime event is published on. The publisher is responsible for translating
/// these into transport-specific groups (e.g. SignalR group names like <c>channel:{id}</c>).
/// </summary>
public enum RealtimeTarget
{
    /// <summary>One user, regardless of which workspace they're viewing.</summary>
    User,

    /// <summary>Every connection currently joined to a chat channel.</summary>
    Channel,

    /// <summary>Every connection currently scoped to a workspace.</summary>
    Workspace,
}

/// <summary>
/// A pending realtime event captured during a unit of work. Events are buffered and only
/// flushed to the transport after <c>SaveChanges</c> succeeds, so listeners never observe
/// state that hasn't actually committed.
/// </summary>
public sealed record RealtimeEvent(
    RealtimeTarget Target,
    Guid TargetId,
    string EventName,
    object Payload
);
