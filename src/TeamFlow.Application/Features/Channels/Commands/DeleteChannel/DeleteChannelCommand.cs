using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Channels.Services;
using TeamFlow.Domain.Discussions;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Channels.Commands.DeleteChannel;

/// <summary>
/// Hard-deletes a channel and purges its messages. Direct-message channels are not allowed
/// to be deleted via this endpoint; they live for the lifetime of the workspace.
/// </summary>
public sealed record DeleteChannelCommand(Guid ChannelId) : ICommand;

internal sealed class DeleteChannelHandler : ICommandHandler<DeleteChannelCommand>
{
    private readonly IChannelRepository _channels;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;
    private readonly IMessagePurger _purger;

    public DeleteChannelHandler(
        IChannelRepository channels,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser,
        IMessagePurger purger
    )
    {
        _channels = channels;
        _workspaces = workspaces;
        _currentUser = currentUser;
        _purger = purger;
    }

    public async Task<Result> Handle(DeleteChannelCommand request, CancellationToken ct)
    {
        var channel = await _channels.GetByIdAsync(request.ChannelId, ct);
        if (channel is null)
            return Error.NotFound("Channel not found.");
        if (channel.Type == ChannelType.Direct)
            return Error.Validation("Direct-message channels cannot be deleted.");

        var actor = _currentUser.RequireUserId();
        if (
            !ChannelAuthorization.IsCreator(channel, actor)
            && !await _workspaces.IsOwnerOrAdminAsync(channel.WorkspaceId, actor, ct)
        )
            return Error.Forbidden(
                "Only the channel creator or workspace owners/admins can delete channels."
            );

        await _purger.PurgeMessagesForChannelAsync(channel.Id, ct);
        _channels.Remove(channel);
        return Result.Success();
    }
}
