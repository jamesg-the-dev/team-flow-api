using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Messages.DTOs;
using TeamFlow.Domain.Discussions;

namespace TeamFlow.Application.Features.Messages.Queries.GetMessage;

public sealed record GetMessageQuery(Guid MessageId) : IQuery<MessageDto>;

internal sealed class GetMessageHandler : IQueryHandler<GetMessageQuery, MessageDto>
{
    private readonly IMessageRepository _messages;
    private readonly IChannelRepository _channels;
    private readonly ICurrentUser _currentUser;
    private readonly IMessageReplyCountService _replies;

    public GetMessageHandler(
        IMessageRepository messages,
        IChannelRepository channels,
        ICurrentUser currentUser,
        IMessageReplyCountService replies
    )
    {
        _messages = messages;
        _channels = channels;
        _currentUser = currentUser;
        _replies = replies;
    }

    public async Task<Result<MessageDto>> Handle(GetMessageQuery request, CancellationToken ct)
    {
        var message = await _messages.GetByIdAsync(request.MessageId, ct);
        if (message is null)
            return Error.NotFound("Message not found.");

        var actor = _currentUser.RequireUserId();
        if (!await _channels.IsMemberAsync(message.ChannelId, actor, ct))
            return Error.Forbidden("You are not a member of this channel.");

        var replyCount =
            message.ParentId is null ? await _replies.CountAsync(message.Id, ct) : 0;
        return message.ToDto(replyCount);
    }
}

/// <summary>Read-side helper to fetch reply counts for root messages without loading replies.</summary>
public interface IMessageReplyCountService
{
    Task<int> CountAsync(Guid parentId, CancellationToken ct);
}
