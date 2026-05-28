using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Realtime;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Discussions;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Messages.Commands.DeleteMessage;

/// <summary>
/// Soft-deletes a message. Permitted to: the author, the channel creator, or a workspace
/// owner/admin.
/// </summary>
public sealed record DeleteMessageCommand(Guid MessageId) : ICommand;

internal sealed class DeleteMessageHandler : ICommandHandler<DeleteMessageCommand>
{
    private readonly IMessageRepository _messages;
    private readonly IChannelRepository _channels;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IRealtimePublishQueue _realtime;

    public DeleteMessageHandler(
        IMessageRepository messages,
        IChannelRepository channels,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IRealtimePublishQueue realtime
    )
    {
        _messages = messages;
        _channels = channels;
        _workspaces = workspaces;
        _currentUser = currentUser;
        _clock = clock;
        _realtime = realtime;
    }

    public async Task<Result> Handle(DeleteMessageCommand request, CancellationToken ct)
    {
        var message = await _messages.GetByIdAsync(request.MessageId, ct);
        if (message is null)
            return Error.NotFound("Message not found.");

        var actor = _currentUser.RequireUserId();
        if (actor != message.AuthorId)
        {
            var channel = await _channels.GetByIdAsync(message.ChannelId, ct);
            if (channel is null)
                return Error.NotFound("Channel not found.");
            if (
                !ChannelAuthorization.IsCreator(channel, actor)
                && !await _workspaces.IsOwnerOrAdminAsync(channel.WorkspaceId, actor, ct)
            )
                return Error.Forbidden(
                    "Only the author, the channel creator, or a workspace owner/admin can delete this message."
                );
        }

        message.SoftDelete(_clock.UtcNow);
        _realtime.Enqueue(
            new RealtimeEvent(
                RealtimeTarget.Channel,
                message.ChannelId,
                RealtimeEvents.MessageDeleted,
                new { messageId = message.Id, channelId = message.ChannelId }
            )
        );
        return Result.Success();
    }
}
