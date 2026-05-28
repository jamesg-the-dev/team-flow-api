using TeamFlow.Application.Features.Messages.DTOs;
using TeamFlow.Domain.Discussions;

namespace TeamFlow.Application.Features.Messages;

public static class MessageMapping
{
    public static MessageDto ToDto(this Message m, int replyCount = 0) =>
        new(
            m.Id,
            m.ChannelId,
            m.AuthorId,
            m.ParentId,
            m.Body,
            m.IsPinned,
            m.CreatedAt,
            m.EditedAt,
            m.Reactions
                .Select(r => new ReactionDto(r.UserId, r.Emoji, r.CreatedAt))
                .ToList(),
            m.Mentions.Select(x => x.UserId).ToList(),
            replyCount
        );
}
