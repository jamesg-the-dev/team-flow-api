using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Discussions;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Channels.Commands.RemoveChannelMember;

/// <summary>
/// Removes a user from a channel. A user can always remove themselves (leave); otherwise the
/// caller must be the channel creator or a workspace owner/admin.
/// </summary>
public sealed record RemoveChannelMemberCommand(Guid ChannelId, Guid UserId) : ICommand;

internal sealed class RemoveChannelMemberHandler : ICommandHandler<RemoveChannelMemberCommand>
{
    private readonly IChannelRepository _channels;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public RemoveChannelMemberHandler(
        IChannelRepository channels,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _channels = channels;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RemoveChannelMemberCommand request, CancellationToken ct)
    {
        var channel = await _channels.GetByIdAsync(request.ChannelId, ct);
        if (channel is null)
            return Error.NotFound("Channel not found.");
        if (channel.Type == ChannelType.Direct)
            return Error.Validation("Direct-message channels do not allow member removal.");

        var actor = _currentUser.RequireUserId();
        var isSelf = actor == request.UserId;
        if (!isSelf)
        {
            if (
                !ChannelAuthorization.IsCreator(channel, actor)
                && !await _workspaces.IsOwnerOrAdminAsync(channel.WorkspaceId, actor, ct)
            )
                return Error.Forbidden(
                    "Only the channel creator or workspace owners/admins can remove other members."
                );
        }

        if (!ChannelAuthorization.IsMember(channel, request.UserId))
            return Result.Success(); // idempotent

        channel.Leave(request.UserId);
        return Result.Success();
    }
}
