using TeamFlow.Domain.Enums;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Discussions;

/// <summary>
/// Channel aggregate. Owns membership; messages are a separate aggregate to keep this small
/// (channels can have millions of messages — they don't share a consistency boundary).
/// </summary>
public sealed class Channel : AggregateRoot
{
    private readonly List<ChannelMember> _members = new();

    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Topic { get; private set; }
    public ChannelType Type { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

    public IReadOnlyCollection<ChannelMember> Members => _members.AsReadOnly();

    private Channel() { }

    public static Channel Create(
        Guid workspaceId,
        string name,
        ChannelType type,
        Guid createdBy,
        string? topic = null
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            throw DomainException.Invariant("Channel name required.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^[a-z0-9][a-z0-9-_]{0,79}$"))
            throw DomainException.Invariant(
                "Channel name must be lowercase alphanumerics, dashes or underscores."
            );

        var ch = new Channel
        {
            Id = Guid.CreateVersion7(),
            WorkspaceId = workspaceId,
            Name = name,
            Topic = topic,
            Type = type,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        ch._members.Add(new ChannelMember(ch.Id, createdBy));
        return ch;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw DomainException.Invariant("Channel name required.");
        Name = name;
    }

    public void SetTopic(string? topic) => Topic = topic;

    public void Archive(DateTimeOffset at)
    {
        if (ArchivedAt is not null)
            return;
        ArchivedAt = at;
    }

    public ChannelMember Join(Guid userId)
    {
        if (_members.Any(m => m.UserId == userId))
            return _members.First(m => m.UserId == userId);
        var member = new ChannelMember(Id, userId);
        _members.Add(member);
        return member;
    }

    public void Leave(Guid userId) => _members.RemoveAll(m => m.UserId == userId);

    public void MarkRead(Guid userId, DateTimeOffset at)
    {
        var m =
            _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw DomainException.Invariant("User is not a member of this channel.");
        m.MarkRead(at);
    }

    /// <summary>Mute / unmute notifications for a member of this channel.</summary>
    public void SetMute(Guid userId, bool muted)
    {
        var m =
            _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw DomainException.Invariant("User is not a member of this channel.");
        if (muted) m.Mute();
        else m.Unmute();
    }
}

public sealed class ChannelMember
{
    public Guid ChannelId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }
    public DateTimeOffset LastReadAt { get; private set; }
    public bool IsMuted { get; private set; }

    private ChannelMember() { }

    internal ChannelMember(Guid channelId, Guid userId)
    {
        ChannelId = channelId;
        UserId = userId;
        JoinedAt = DateTimeOffset.UtcNow;
        LastReadAt = JoinedAt;
    }

    internal void MarkRead(DateTimeOffset at)
    {
        if (at > LastReadAt)
            LastReadAt = at;
    }

    public void Mute() => IsMuted = true;

    public void Unmute() => IsMuted = false;
}

public interface IChannelRepository : IRepository<Channel>
{
    Task<Channel?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> NameExistsAsync(Guid workspaceId, string name, CancellationToken ct = default);

    /// <summary>Cheap channel-membership check used by handlers for authorization.</summary>
    Task<bool> IsMemberAsync(Guid channelId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Finds an existing 1-1 direct-message channel between exactly the two users in a workspace,
    /// or <c>null</c> if none exists. Used to make DM creation idempotent.
    /// </summary>
    Task<Channel?> FindDirectChannelAsync(
        Guid workspaceId,
        Guid userA,
        Guid userB,
        CancellationToken ct = default
    );

    void Add(Channel channel);
    void Remove(Channel channel);
}
