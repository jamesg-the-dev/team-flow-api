using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Realtime;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Discussions;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Messages.Commands.SetMessagePin;

/// <summary>Pins or unpins a message. Restricted to channel mod (creator or workspace owner/admin).</summary>
public sealed record SetMessagePinCommand(Guid MessageId, bool Pinned) : ICommand;

internal sealed class SetMessagePinHandler : ICommandHandler<SetMessagePinCommand>
{
    private readonly IMessageRepository _messages;
    private readonly IChannelRepository _channels;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;
    private readonly IRealtimePublishQueue _realtime;

    public SetMessagePinHandler(
        IMessageRepository messages,
        IChannelRepository channels,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser,
        IRealtimePublishQueue realtime
    )
    {
        _messages = messages;
        _channels = channels;
        _workspaces = workspaces;
        _currentUser = currentUser;
        _realtime = realtime;
    }

    public async Task<Result> Handle(SetMessagePinCommand request, CancellationToken ct)
    {
        var message = await _messages.GetByIdAsync(request.MessageId, ct);
        if (message is null)
            return Error.NotFound("Message not found.");

        var channel = await _channels.GetByIdAsync(message.ChannelId, ct);
        if (channel is null)
            return Error.NotFound("Channel not found.");

        var actor = _currentUser.RequireUserId();
        if (
            !ChannelAuthorization.IsCreator(channel, actor)
            && !await _workspaces.IsOwnerOrAdminAsync(channel.WorkspaceId, actor, ct)
        )
            return Error.Forbidden(
                "Only the channel creator or workspace owners/admins can pin or unpin messages."
            );

        if (request.Pinned) message.Pin();
        else message.Unpin();
        _realtime.Enqueue(
            new RealtimeEvent(
                RealtimeTarget.Channel,
                message.ChannelId,
                RealtimeEvents.MessagePinned,
                new { messageId = message.Id, channelId = message.ChannelId, pinned = request.Pinned }
            )
        );
        return Result.Success();
    }
}
