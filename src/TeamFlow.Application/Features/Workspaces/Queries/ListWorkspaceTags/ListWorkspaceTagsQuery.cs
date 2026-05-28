using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Workspaces.DTOs;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Workspaces.Queries.ListWorkspaceTags;

public sealed record ListWorkspaceTagsQuery(Guid WorkspaceId) : IQuery<IReadOnlyList<TagDto>>;

public interface IListWorkspaceTagsQueryService
{
    Task<IReadOnlyList<TagDto>> ExecuteAsync(Guid workspaceId, CancellationToken ct);
}

internal sealed class ListWorkspaceTagsHandler
    : IQueryHandler<ListWorkspaceTagsQuery, IReadOnlyList<TagDto>>
{
    private readonly IListWorkspaceTagsQueryService _service;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public ListWorkspaceTagsHandler(
        IListWorkspaceTagsQueryService service,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _service = service;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<TagDto>>> Handle(
        ListWorkspaceTagsQuery request,
        CancellationToken ct
    )
    {
        if (!await _workspaces.IsMemberAsync(request.WorkspaceId, _currentUser.RequireUserId(), ct))
            return Error.Forbidden("You are not a member of this workspace.");

        return Result.Success(await _service.ExecuteAsync(request.WorkspaceId, ct));
    }
}
