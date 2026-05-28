using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Discussions;

namespace TeamFlow.Application.Features.Channels.Commands.MarkChannelRead;

/// <summary>Marks the current user's last-read marker at "now" for the given channel.</summary>
public sealed record MarkChannelReadCommand(Guid ChannelId) : ICommand;

internal sealed class MarkChannelReadHandler : ICommandHandler<MarkChannelReadCommand>
{
    private readonly IChannelRepository _channels;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public MarkChannelReadHandler(
        IChannelRepository channels,
        ICurrentUser currentUser,
        IDateTimeProvider clock
    )
    {
        _channels = channels;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(MarkChannelReadCommand request, CancellationToken ct)
    {
        var channel = await _channels.GetByIdAsync(request.ChannelId, ct);
        if (channel is null)
            return Error.NotFound("Channel not found.");

        var actor = _currentUser.RequireUserId();
        if (!ChannelAuthorization.IsMember(channel, actor))
            return Error.Forbidden("You are not a member of this channel.");

        channel.MarkRead(actor, _clock.UtcNow);
        return Result.Success();
    }
}
