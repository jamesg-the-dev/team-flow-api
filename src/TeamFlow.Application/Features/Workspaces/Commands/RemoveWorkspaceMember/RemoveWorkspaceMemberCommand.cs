using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Workspaces.Commands.RemoveWorkspaceMember;

/// <summary>
/// Removes a member from a workspace. Owners/admins can remove anyone (except the owner);
/// users may always remove themselves.
/// </summary>
public sealed record RemoveWorkspaceMemberCommand(Guid WorkspaceId, Guid UserId) : ICommand;

internal sealed class RemoveWorkspaceMemberHandler
    : ICommandHandler<RemoveWorkspaceMemberCommand>
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public RemoveWorkspaceMemberHandler(IWorkspaceRepository workspaces, ICurrentUser currentUser)
    {
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        RemoveWorkspaceMemberCommand request,
        CancellationToken ct
    )
    {
        var workspace = await _workspaces.GetByIdAsync(request.WorkspaceId, ct);
        if (workspace is null)
            return Error.NotFound("Workspace not found.");

        var callerId = _currentUser.RequireUserId();
        var removingSelf = callerId == request.UserId;

        if (!removingSelf && !WorkspaceAuthorization.IsOwnerOrAdmin(workspace, callerId))
            return Error.Forbidden("Only workspace owners or admins can remove other members.");

        workspace.RemoveMember(request.UserId);
        return Result.Success();
    }
}
