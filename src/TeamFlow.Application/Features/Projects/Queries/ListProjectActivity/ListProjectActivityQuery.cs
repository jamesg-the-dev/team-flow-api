using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Pagination;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Projects.DTOs;
using TeamFlow.Domain.Projects;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Projects.Queries.ListProjectActivity;

public sealed record ListProjectActivityQuery(Guid ProjectId, PaginationRequest Pagination)
    : IQuery<PagedResult<ProjectActivityDto>>;

public interface IListProjectActivityQueryService
{
    Task<PagedResult<ProjectActivityDto>> ExecuteAsync(
        Guid projectId,
        PaginationRequest pagination,
        CancellationToken ct
    );
}

internal sealed class ListProjectActivityHandler
    : IQueryHandler<ListProjectActivityQuery, PagedResult<ProjectActivityDto>>
{
    private readonly IListProjectActivityQueryService _service;
    private readonly IProjectRepository _projects;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public ListProjectActivityHandler(
        IListProjectActivityQueryService service,
        IProjectRepository projects,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _service = service;
        _projects = projects;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<ProjectActivityDto>>> Handle(
        ListProjectActivityQuery request,
        CancellationToken ct
    )
    {
        var project = await _projects.GetByIdAsync(request.ProjectId, ct);
        if (project is null)
            return Error.NotFound("Project not found.");
        if (!await _workspaces.IsMemberAsync(project.WorkspaceId, _currentUser.RequireUserId(), ct))
            return Error.Forbidden("You are not a member of this workspace.");

        return Result.Success(
            await _service.ExecuteAsync(request.ProjectId, request.Pagination, ct)
        );
    }
}
