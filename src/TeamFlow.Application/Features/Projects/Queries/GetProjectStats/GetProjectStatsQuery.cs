using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Projects.DTOs;
using TeamFlow.Domain.Projects;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Projects.Queries.GetProjectStats;

public sealed record GetProjectStatsQuery(Guid ProjectId) : IQuery<ProjectStatsDto>;

public interface IGetProjectStatsQueryService
{
    Task<ProjectStatsDto> ExecuteAsync(Guid projectId, CancellationToken ct);
}

internal sealed class GetProjectStatsHandler
    : IQueryHandler<GetProjectStatsQuery, ProjectStatsDto>
{
    private readonly IGetProjectStatsQueryService _service;
    private readonly IProjectRepository _projects;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public GetProjectStatsHandler(
        IGetProjectStatsQueryService service,
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

    public async Task<Result<ProjectStatsDto>> Handle(
        GetProjectStatsQuery request,
        CancellationToken ct
    )
    {
        var project = await _projects.GetByIdAsync(request.ProjectId, ct);
        if (project is null)
            return Error.NotFound("Project not found.");
        if (!await _workspaces.IsMemberAsync(project.WorkspaceId, _currentUser.RequireUserId(), ct))
            return Error.Forbidden("You are not a member of this workspace.");

        return Result.Success(await _service.ExecuteAsync(request.ProjectId, ct));
    }
}
