using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Channels.DTOs;
using TeamFlow.Domain.Discussions;

namespace TeamFlow.Application.Features.Channels.Queries.ListChannelMembers;

public sealed record ListChannelMembersQuery(Guid ChannelId)
    : IQuery<IReadOnlyList<ChannelMemberDto>>;

internal sealed class ListChannelMembersHandler
    : IQueryHandler<ListChannelMembersQuery, IReadOnlyList<ChannelMemberDto>>
{
    private readonly IChannelRepository _channels;
    private readonly ICurrentUser _currentUser;

    public ListChannelMembersHandler(IChannelRepository channels, ICurrentUser currentUser)
    {
        _channels = channels;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ChannelMemberDto>>> Handle(
        ListChannelMembersQuery request,
        CancellationToken ct
    )
    {
        var channel = await _channels.GetByIdAsync(request.ChannelId, ct);
        if (channel is null)
            return Error.NotFound("Channel not found.");

        var actor = _currentUser.RequireUserId();
        if (!channel.Members.Any(m => m.UserId == actor))
            return Error.Forbidden("You are not a member of this channel.");

        var members = channel
            .Members.Select(m => new ChannelMemberDto(m.UserId, m.JoinedAt, m.LastReadAt, m.IsMuted))
            .ToList();
        return Result.Success<IReadOnlyList<ChannelMemberDto>>(members);
    }
}
