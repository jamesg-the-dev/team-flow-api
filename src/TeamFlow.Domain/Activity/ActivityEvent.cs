using System.Text.Json;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Activity;

/// <summary>
/// Append-only activity event. Powers dashboards and recent-activity feeds.
/// The numeric Id is allocated by the database (bigint identity).
/// </summary>
public sealed class ActivityEvent : IAggregateRoot
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public long Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid? ActorId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public string Verb { get; private set; } = null!;
    public string TargetKind { get; private set; } = null!;
    public Guid TargetId { get; private set; }
    public JsonDocument Metadata { get; private set; } = JsonDocument.Parse("{}");
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => Array.Empty<IDomainEvent>();
    public void ClearDomainEvents() { /* no-op: append-only */ }

    private ActivityEvent() { }

    public static ActivityEvent Record(Guid workspaceId, string verb, string targetKind, Guid targetId,
        Guid? actorId = null, Guid? projectId = null, object? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(verb)) throw DomainException.Invariant("Verb required.");
        if (string.IsNullOrWhiteSpace(targetKind)) throw DomainException.Invariant("Target kind required.");
        return new ActivityEvent
        {
            WorkspaceId = workspaceId,
            ActorId = actorId,
            ProjectId = projectId,
            Verb = verb,
            TargetKind = targetKind,
            TargetId = targetId,
            Metadata = metadata is null
                ? JsonDocument.Parse("{}")
                : JsonDocument.Parse(JsonSerializer.Serialize(metadata, JsonOpts)),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}

public interface IActivityEventRepository : IRepository<ActivityEvent>
{
    Task<IReadOnlyList<ActivityEvent>> ListForWorkspaceAsync(Guid workspaceId, int skip, int take, CancellationToken ct = default);
    Task<IReadOnlyList<ActivityEvent>> ListForProjectAsync(Guid projectId, int skip, int take, CancellationToken ct = default);
    void Add(ActivityEvent activityEvent);
}
