using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Messages.DTOs;
using TeamFlow.Domain.Discussions;

namespace TeamFlow.Application.Features.Messages.Queries.ListChannelMessages;

/// <summary>
/// Channel-timeline page. Returns up to <paramref name="Take"/> root messages with
/// <c>CreatedAt &lt; Before</c> (when supplied), ordered newest-first.
/// </summary>
public sealed record ListChannelMessagesQuery(Guid ChannelId, DateTimeOffset? Before, int Take)
    : IQuery<IReadOnlyList<MessageDto>>;

public interface IListChannelMessagesQueryService
{
    Task<IReadOnlyList<MessageDto>> ExecuteAsync(
        Guid channelId,
        DateTimeOffset? before,
        int take,
        CancellationToken ct
    );
}

internal sealed class ListChannelMessagesHandler
    : IQueryHandler<ListChannelMessagesQuery, IReadOnlyList<MessageDto>>
{
    private const int MaxTake = 100;
    private const int DefaultTake = 50;

    private readonly IListChannelMessagesQueryService _svc;
    private readonly IChannelRepository _channels;
    private readonly ICurrentUser _currentUser;

    public ListChannelMessagesHandler(
        IListChannelMessagesQueryService svc,
        IChannelRepository channels,
        ICurrentUser currentUser
    )
    {
        _svc = svc;
        _channels = channels;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<MessageDto>>> Handle(
        ListChannelMessagesQuery request,
        CancellationToken ct
    )
    {
        var actor = _currentUser.RequireUserId();
        if (!await _channels.IsMemberAsync(request.ChannelId, actor, ct))
            return Error.Forbidden("You are not a member of this channel.");

        var take = request.Take <= 0 ? DefaultTake : Math.Min(request.Take, MaxTake);
        var rows = await _svc.ExecuteAsync(request.ChannelId, request.Before, take, ct);
        return Result.Success(rows);
    }
}
