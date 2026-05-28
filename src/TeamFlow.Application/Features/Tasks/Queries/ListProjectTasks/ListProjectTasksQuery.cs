using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Pagination;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Projects;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Tasks.Queries.ListProjectTasks;

public sealed record ListProjectTasksQuery(
    Guid ProjectId,
    TaskColumn? Column,
    Guid? AssigneeId,
    PriorityLevel? Priority,
    string? Search,
    PaginationRequest Pagination
) : IQuery<PagedResult<TaskDto>>;

public interface IListProjectTasksQueryService
{
    Task<PagedResult<TaskDto>> ExecuteAsync(ListProjectTasksQuery query, CancellationToken ct);
}

internal sealed class ListProjectTasksHandler
    : IQueryHandler<ListProjectTasksQuery, PagedResult<TaskDto>>
{
    private readonly IListProjectTasksQueryService _service;
    private readonly IProjectRepository _projects;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public ListProjectTasksHandler(
        IListProjectTasksQueryService service,
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

    public async Task<Result<PagedResult<TaskDto>>> Handle(
        ListProjectTasksQuery request,
        CancellationToken ct
    )
    {
        var project = await _projects.GetByIdAsync(request.ProjectId, ct);
        if (project is null)
            return Error.NotFound("Project not found.");
        if (!await _workspaces.IsMemberAsync(project.WorkspaceId, _currentUser.RequireUserId(), ct))
            return Error.Forbidden("You are not a member of this workspace.");

        return Result.Success(await _service.ExecuteAsync(request, ct));
    }
}
