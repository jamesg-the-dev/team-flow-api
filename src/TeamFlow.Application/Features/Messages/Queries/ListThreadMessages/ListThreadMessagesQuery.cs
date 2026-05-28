using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Messages.DTOs;
using TeamFlow.Domain.Discussions;

namespace TeamFlow.Application.Features.Messages.Queries.ListThreadMessages;

/// <summary>
/// Returns the root message plus its replies, ordered chronologically. Reply count is implicit
/// (caller can take <c>list.Count - 1</c>).
/// </summary>
public sealed record ListThreadMessagesQuery(Guid ParentMessageId)
    : IQuery<IReadOnlyList<MessageDto>>;

public interface IListThreadMessagesQueryService
{
    Task<IReadOnlyList<MessageDto>> ExecuteAsync(Guid parentMessageId, CancellationToken ct);
}

internal sealed class ListThreadMessagesHandler
    : IQueryHandler<ListThreadMessagesQuery, IReadOnlyList<MessageDto>>
{
    private readonly IListThreadMessagesQueryService _svc;
    private readonly IMessageRepository _messages;
    private readonly IChannelRepository _channels;
    private readonly ICurrentUser _currentUser;

    public ListThreadMessagesHandler(
        IListThreadMessagesQueryService svc,
        IMessageRepository messages,
        IChannelRepository channels,
        ICurrentUser currentUser
    )
    {
        _svc = svc;
        _messages = messages;
        _channels = channels;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<MessageDto>>> Handle(
        ListThreadMessagesQuery request,
        CancellationToken ct
    )
    {
        var parent = await _messages.GetByIdAsync(request.ParentMessageId, ct);
        if (parent is null)
            return Error.NotFound("Parent message not found.");
        if (parent.ParentId is not null)
            return Error.Validation("Provided id is not a thread root.");

        var actor = _currentUser.RequireUserId();
        if (!await _channels.IsMemberAsync(parent.ChannelId, actor, ct))
            return Error.Forbidden("You are not a member of this channel.");

        var rows = await _svc.ExecuteAsync(request.ParentMessageId, ct);
        return Result.Success(rows);
    }
}
