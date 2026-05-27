using TeamFlow.Domain.Enums;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Notifications;

public sealed class Notification : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public Guid RecipientId { get; private set; }
    public Guid? ActorId { get; private set; }
    public NotificationKind Kind { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Body { get; private set; }
    public string? TargetKind { get; private set; }
    public Guid? TargetId { get; private set; }
    public string? Url { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Notification() { }

    public static Notification Create(
        Guid workspaceId,
        Guid recipientId,
        NotificationKind kind,
        string title,
        Guid? actorId = null,
        string? body = null,
        string? targetKind = null,
        Guid? targetId = null,
        string? url = null
    )
    {
        if (string.IsNullOrWhiteSpace(title))
            throw DomainException.Invariant("Title required.");
        return new Notification
        {
            Id = Guid.CreateVersion7(),
            WorkspaceId = workspaceId,
            RecipientId = recipientId,
            ActorId = actorId,
            Kind = kind,
            Title = title,
            Body = body,
            TargetKind = targetKind,
            TargetId = targetId,
            Url = url,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void MarkRead(DateTimeOffset at)
    {
        if (ReadAt is null)
            ReadAt = at;
    }
}

public sealed class NotificationPreference
{
    public Guid UserId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public NotificationKind Kind { get; private set; }
    public DeliveryChannel Channel { get; private set; }
    public bool Enabled { get; private set; }

    private NotificationPreference() { }

    public NotificationPreference(
        Guid userId,
        Guid workspaceId,
        NotificationKind kind,
        DeliveryChannel channel,
        bool enabled = true
    )
    {
        UserId = userId;
        WorkspaceId = workspaceId;
        Kind = kind;
        Channel = channel;
        Enabled = enabled;
    }

    public void Enable() => Enabled = true;

    public void Disable() => Enabled = false;
}

public interface INotificationRepository : IRepository<Notification>
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> ListInboxAsync(
        Guid recipientId,
        bool unreadOnly,
        int skip,
        int take,
        CancellationToken ct = default
    );
    Task<int> CountUnreadAsync(Guid recipientId, CancellationToken ct = default);
    void Add(Notification notification);
}

public interface INotificationPreferenceRepository
{
    /// <summary>All preferences for a single user across every workspace they belong to.</summary>
    Task<IReadOnlyList<NotificationPreference>> ListForUserAsync(
        Guid userId,
        CancellationToken ct = default
    );

    /// <summary>Preferences for a user inside a specific workspace.</summary>
    Task<IReadOnlyList<NotificationPreference>> ListForUserAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken ct = default
    );

    void Add(NotificationPreference preference);
    void Remove(NotificationPreference preference);
    void RemoveRange(IEnumerable<NotificationPreference> preferences);
}
