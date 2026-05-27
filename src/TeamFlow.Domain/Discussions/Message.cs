using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Discussions;

/// <summary>
/// Message aggregate root. Reactions, mentions live inside the aggregate as small collections.
/// Threading is modelled by ParentId (root message of the thread).
/// </summary>
public sealed class Message : AggregateRoot, ISoftDeletable
{
    private readonly List<MessageReaction> _reactions = new();
    private readonly List<MessageMention> _mentions = new();

    public Guid ChannelId { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Body { get; private set; } = null!;
    public bool IsPinned { get; private set; }
    public DateTimeOffset? EditedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public IReadOnlyCollection<MessageReaction> Reactions => _reactions.AsReadOnly();
    public IReadOnlyCollection<MessageMention> Mentions => _mentions.AsReadOnly();

    private Message() { }

    public static Message Post(
        Guid channelId,
        Guid authorId,
        string body,
        Guid? parentId = null,
        IEnumerable<Guid>? mentions = null
    )
    {
        if (string.IsNullOrWhiteSpace(body))
            throw DomainException.Invariant("Message body is required.");
        if (body.Length > 10_000)
            throw DomainException.Invariant("Message body too long.");

        var msg = new Message
        {
            Id = Guid.CreateVersion7(),
            ChannelId = channelId,
            AuthorId = authorId,
            Body = body,
            ParentId = parentId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        if (mentions is not null)
            foreach (var u in mentions.Distinct())
                msg._mentions.Add(new MessageMention(msg.Id, u));

        return msg;
    }

    public void Edit(Guid editorId, string body, DateTimeOffset now)
    {
        if (editorId != AuthorId)
            throw DomainException.Invariant("Only the author can edit a message.");
        if (string.IsNullOrWhiteSpace(body))
            throw DomainException.Invariant("Body required.");
        Body = body;
        EditedAt = now;
    }

    public void Pin() => IsPinned = true;

    public void Unpin() => IsPinned = false;

    public void React(Guid userId, string emoji)
    {
        if (string.IsNullOrWhiteSpace(emoji))
            throw DomainException.Invariant("Emoji required.");
        if (_reactions.Any(r => r.UserId == userId && r.Emoji == emoji))
            return;
        _reactions.Add(new MessageReaction(Id, userId, emoji));
    }

    public void Unreact(Guid userId, string emoji) =>
        _reactions.RemoveAll(r => r.UserId == userId && r.Emoji == emoji);

    public void SoftDelete(DateTimeOffset at) => DeletedAt = at;
}

public sealed class MessageReaction
{
    public Guid MessageId { get; private set; }
    public Guid UserId { get; private set; }
    public string Emoji { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private MessageReaction() { }

    internal MessageReaction(Guid messageId, Guid userId, string emoji)
    {
        MessageId = messageId;
        UserId = userId;
        Emoji = emoji;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class MessageMention
{
    public Guid MessageId { get; private set; }
    public Guid UserId { get; private set; }

    private MessageMention() { }

    internal MessageMention(Guid messageId, Guid userId)
    {
        MessageId = messageId;
        UserId = userId;
    }
}

public interface IMessageRepository : IRepository<Message>
{
    Task<Message?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Message>> GetChannelTimelineAsync(
        Guid channelId,
        DateTimeOffset? before,
        int take,
        CancellationToken ct = default
    );
    Task<IReadOnlyList<Message>> GetThreadAsync(Guid parentId, CancellationToken ct = default);
    void Add(Message message);
    void Remove(Message message);
}
