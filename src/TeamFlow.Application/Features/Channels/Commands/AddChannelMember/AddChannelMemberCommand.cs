using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Discussions;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.SeedWork;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Channels.Commands.AddChannelMember;

public sealed record AddChannelMemberCommand(Guid ChannelId, Guid UserId) : ICommand;

internal sealed class AddChannelMemberHandler : ICommandHandler<AddChannelMemberCommand>
{
    private readonly IChannelRepository _channels;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public AddChannelMemberHandler(
        IChannelRepository channels,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _channels = channels;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(AddChannelMemberCommand request, CancellationToken ct)
    {
        var channel = await _channels.GetByIdAsync(request.ChannelId, ct);
        if (channel is null)
            return Error.NotFound("Channel not found.");
        if (channel.Type == ChannelType.Direct)
            return Error.Validation("Members cannot be added to a direct-message channel.");

        var actor = _currentUser.RequireUserId();
        if (
            !ChannelAuthorization.IsCreator(channel, actor)
            && !await _workspaces.IsOwnerOrAdminAsync(channel.WorkspaceId, actor, ct)
        )
            return Error.Forbidden(
                "Only the channel creator or workspace owners/admins can invite members."
            );

        if (!await _workspaces.IsMemberAsync(channel.WorkspaceId, request.UserId, ct))
            return Error.Validation("User is not a member of this workspace.");

        if (ChannelAuthorization.IsMember(channel, request.UserId))
            return Result.Success(); // idempotent

        try
        {
            channel.Join(request.UserId);
        }
        catch (DomainException ex)
        {
            return Error.Conflict(ex.Message);
        }

        return Result.Success();
    }
}
