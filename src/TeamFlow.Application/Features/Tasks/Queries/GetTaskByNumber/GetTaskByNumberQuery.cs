using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Domain.Projects;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Tasks.Queries.GetTaskByNumber;

public sealed record GetTaskByNumberQuery(Guid ProjectId, int Number) : IQuery<TaskDto>;

public interface IGetTaskByNumberQueryService
{
    Task<TaskDto?> ExecuteAsync(Guid projectId, int number, CancellationToken ct);
}

internal sealed class GetTaskByNumberHandler : IQueryHandler<GetTaskByNumberQuery, TaskDto>
{
    private readonly IGetTaskByNumberQueryService _service;
    private readonly IProjectRepository _projects;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public GetTaskByNumberHandler(
        IGetTaskByNumberQueryService service,
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

    public async Task<Result<TaskDto>> Handle(
        GetTaskByNumberQuery request,
        CancellationToken ct
    )
    {
        var project = await _projects.GetByIdAsync(request.ProjectId, ct);
        if (project is null)
            return Error.NotFound("Project not found.");
        if (!await _workspaces.IsMemberAsync(project.WorkspaceId, _currentUser.RequireUserId(), ct))
            return Error.Forbidden("You are not a member of this workspace.");

        var dto = await _service.ExecuteAsync(request.ProjectId, request.Number, ct);
        return dto is null ? Error.NotFound("Task not found.") : Result.Success(dto);
    }
}
