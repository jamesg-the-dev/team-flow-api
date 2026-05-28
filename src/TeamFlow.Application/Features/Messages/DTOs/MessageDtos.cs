namespace TeamFlow.Application.Features.Messages.DTOs;

public sealed record ReactionDto(Guid UserId, string Emoji, DateTimeOffset CreatedAt);

public sealed record MessageDto(
    Guid Id,
    Guid ChannelId,
    Guid AuthorId,
    Guid? ParentId,
    string Body,
    bool IsPinned,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt,
    IReadOnlyList<ReactionDto> Reactions,
    IReadOnlyList<Guid> Mentions,
    int ReplyCount
);
