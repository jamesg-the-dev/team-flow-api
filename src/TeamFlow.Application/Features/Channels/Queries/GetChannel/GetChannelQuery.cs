using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Channels.DTOs;
using TeamFlow.Domain.Discussions;

namespace TeamFlow.Application.Features.Channels.Queries.GetChannel;

public sealed record GetChannelQuery(Guid ChannelId) : IQuery<ChannelDto>;

internal sealed class GetChannelHandler : IQueryHandler<GetChannelQuery, ChannelDto>
{
    private readonly IChannelRepository _channels;
    private readonly ICurrentUser _currentUser;

    public GetChannelHandler(IChannelRepository channels, ICurrentUser currentUser)
    {
        _channels = channels;
        _currentUser = currentUser;
    }

    public async Task<Result<ChannelDto>> Handle(GetChannelQuery request, CancellationToken ct)
    {
        var channel = await _channels.GetByIdAsync(request.ChannelId, ct);
        if (channel is null)
            return Error.NotFound("Channel not found.");

        var actor = _currentUser.RequireUserId();
        if (!channel.Members.Any(m => m.UserId == actor))
            return Error.Forbidden("You are not a member of this channel.");

        return new ChannelDto(
            channel.Id,
            channel.WorkspaceId,
            channel.Name,
            channel.Topic,
            channel.Type,
            channel.CreatedBy,
            channel.CreatedAt,
            channel.Members.Count
        );
    }
}
