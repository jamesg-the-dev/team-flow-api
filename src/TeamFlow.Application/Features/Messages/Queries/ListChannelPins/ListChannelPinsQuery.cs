using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Messages.DTOs;
using TeamFlow.Domain.Discussions;

namespace TeamFlow.Application.Features.Messages.Queries.ListChannelPins;

public sealed record ListChannelPinsQuery(Guid ChannelId) : IQuery<IReadOnlyList<MessageDto>>;

public interface IListChannelPinsQueryService
{
    Task<IReadOnlyList<MessageDto>> ExecuteAsync(Guid channelId, CancellationToken ct);
}

internal sealed class ListChannelPinsHandler
    : IQueryHandler<ListChannelPinsQuery, IReadOnlyList<MessageDto>>
{
    private readonly IListChannelPinsQueryService _svc;
    private readonly IChannelRepository _channels;
    private readonly ICurrentUser _currentUser;

    public ListChannelPinsHandler(
        IListChannelPinsQueryService svc,
        IChannelRepository channels,
        ICurrentUser currentUser
    )
    {
        _svc = svc;
        _channels = channels;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<MessageDto>>> Handle(
        ListChannelPinsQuery request,
        CancellationToken ct
    )
    {
        var actor = _currentUser.RequireUserId();
        if (!await _channels.IsMemberAsync(request.ChannelId, actor, ct))
            return Error.Forbidden("You are not a member of this channel.");
        var rows = await _svc.ExecuteAsync(request.ChannelId, ct);
        return Result.Success(rows);
    }
}
