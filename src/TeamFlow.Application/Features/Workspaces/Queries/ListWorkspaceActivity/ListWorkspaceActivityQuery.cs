using System.Text.Json;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Pagination;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Workspaces.Queries.ListWorkspaceActivity;

public sealed record WorkspaceActivityDto(
    long Id,
    Guid? ActorId,
    Guid? ProjectId,
    string Verb,
    string TargetKind,
    Guid TargetId,
    JsonElement Metadata,
    DateTimeOffset CreatedAt
);

public sealed record ListWorkspaceActivityQuery(Guid WorkspaceId, PaginationRequest Pagination)
    : IQuery<PagedResult<WorkspaceActivityDto>>;

public interface IListWorkspaceActivityQueryService
{
    Task<PagedResult<WorkspaceActivityDto>> ExecuteAsync(
        Guid workspaceId,
        PaginationRequest pagination,
        CancellationToken ct
    );
}

internal sealed class ListWorkspaceActivityHandler
    : IQueryHandler<ListWorkspaceActivityQuery, PagedResult<WorkspaceActivityDto>>
{
    private readonly IListWorkspaceActivityQueryService _svc;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public ListWorkspaceActivityHandler(
        IListWorkspaceActivityQueryService svc,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _svc = svc;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<WorkspaceActivityDto>>> Handle(
        ListWorkspaceActivityQuery request,
        CancellationToken ct
    )
    {
        var userId = _currentUser.RequireUserId();
        if (!await _workspaces.IsMemberAsync(request.WorkspaceId, userId, ct))
            return Error.Forbidden("You are not a member of this workspace.");
        return await _svc.ExecuteAsync(request.WorkspaceId, request.Pagination, ct);
    }
}
