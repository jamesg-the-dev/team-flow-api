using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Discussions;

namespace TeamFlow.Application.Features.Channels.Commands.SetChannelMute;

/// <summary>Mutes or unmutes the current user's membership in a channel.</summary>
public sealed record SetChannelMuteCommand(Guid ChannelId, bool Muted) : ICommand;

internal sealed class SetChannelMuteHandler : ICommandHandler<SetChannelMuteCommand>
{
    private readonly IChannelRepository _channels;
    private readonly ICurrentUser _currentUser;

    public SetChannelMuteHandler(IChannelRepository channels, ICurrentUser currentUser)
    {
        _channels = channels;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(SetChannelMuteCommand request, CancellationToken ct)
    {
        var channel = await _channels.GetByIdAsync(request.ChannelId, ct);
        if (channel is null)
            return Error.NotFound("Channel not found.");

        var actor = _currentUser.RequireUserId();
        if (!ChannelAuthorization.IsMember(channel, actor))
            return Error.Forbidden("You are not a member of this channel.");

        channel.SetMute(actor, request.Muted);
        return Result.Success();
    }
}
