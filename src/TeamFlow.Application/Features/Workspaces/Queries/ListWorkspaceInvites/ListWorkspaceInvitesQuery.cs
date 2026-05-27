using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Workspaces.DTOs;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Workspaces.Queries.ListWorkspaceInvites;

public sealed record ListWorkspaceInvitesQuery(Guid WorkspaceId, bool IncludeExpired = false)
    : IQuery<IReadOnlyList<WorkspaceInviteDto>>;

public interface IListWorkspaceInvitesQueryService
{
    Task<IReadOnlyList<WorkspaceInviteDto>> ExecuteAsync(
        Guid workspaceId,
        bool includeExpired,
        CancellationToken ct
    );
}

internal sealed class ListWorkspaceInvitesHandler
    : IQueryHandler<ListWorkspaceInvitesQuery, IReadOnlyList<WorkspaceInviteDto>>
{
    private readonly IListWorkspaceInvitesQueryService _service;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public ListWorkspaceInvitesHandler(
        IListWorkspaceInvitesQueryService service,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _service = service;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<WorkspaceInviteDto>>> Handle(
        ListWorkspaceInvitesQuery request,
        CancellationToken ct
    )
    {
        var workspace = await _workspaces.GetByIdAsync(request.WorkspaceId, ct);
        if (workspace is null)
            return Error.NotFound("Workspace not found.");
        if (!WorkspaceAuthorization.IsOwnerOrAdmin(workspace, _currentUser.RequireUserId()))
            return Error.Forbidden("Only workspace owners or admins can view invites.");

        var items = await _service.ExecuteAsync(request.WorkspaceId, request.IncludeExpired, ct);
        return Result.Success(items);
    }
}
