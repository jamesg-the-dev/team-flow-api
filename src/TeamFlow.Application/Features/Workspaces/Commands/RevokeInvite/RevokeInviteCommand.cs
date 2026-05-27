using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Workspaces.Commands.RevokeInvite;

public sealed record RevokeInviteCommand(Guid WorkspaceId, Guid InviteId) : ICommand;

internal sealed class RevokeInviteHandler : ICommandHandler<RevokeInviteCommand>
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public RevokeInviteHandler(IWorkspaceRepository workspaces, ICurrentUser currentUser)
    {
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RevokeInviteCommand request, CancellationToken ct)
    {
        var workspace = await _workspaces.GetByIdWithInvitesAsync(request.WorkspaceId, ct);
        if (workspace is null)
            return Error.NotFound("Workspace not found.");

        if (!WorkspaceAuthorization.IsOwnerOrAdmin(workspace, _currentUser.RequireUserId()))
            return Error.Forbidden("Only workspace owners or admins can revoke invites.");

        workspace.RevokeInvite(request.InviteId);
        return Result.Success();
    }
}
