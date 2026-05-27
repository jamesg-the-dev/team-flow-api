using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Workspaces.DTOs;

namespace TeamFlow.Application.Features.Workspaces.Queries.ListWorkspaceMembers;

public sealed record ListWorkspaceMembersQuery(Guid WorkspaceId)
    : IQuery<IReadOnlyList<WorkspaceMemberDto>>;

public interface IListWorkspaceMembersQueryService
{
    Task<IReadOnlyList<WorkspaceMemberDto>> ExecuteAsync(
        Guid workspaceId,
        CancellationToken ct
    );
}

internal sealed class ListWorkspaceMembersHandler
    : IQueryHandler<ListWorkspaceMembersQuery, IReadOnlyList<WorkspaceMemberDto>>
{
    private readonly IListWorkspaceMembersQueryService _service;
    private readonly TeamFlow.Domain.Workspaces.IWorkspaceRepository _workspaces;
    private readonly TeamFlow.Application.Common.Abstractions.ICurrentUser _currentUser;

    public ListWorkspaceMembersHandler(
        IListWorkspaceMembersQueryService service,
        TeamFlow.Domain.Workspaces.IWorkspaceRepository workspaces,
        TeamFlow.Application.Common.Abstractions.ICurrentUser currentUser
    )
    {
        _service = service;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<WorkspaceMemberDto>>> Handle(
        ListWorkspaceMembersQuery request,
        CancellationToken ct
    )
    {
        var userId = _currentUser.RequireUserId();
        if (!await _workspaces.IsMemberAsync(request.WorkspaceId, userId, ct))
            return Error.Forbidden("You are not a member of this workspace.");

        var items = await _service.ExecuteAsync(request.WorkspaceId, ct);
        return Result.Success(items);
    }
}
