using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Projects;

namespace TeamFlow.Application.Features.Projects.Queries.GetProjectVelocity;

/// <summary>One row per ISO-week (Monday-anchored, UTC), with completed-task counts.</summary>
public sealed record VelocityPointDto(DateOnly WeekStart, int Completed, int Created);

public sealed record GetProjectVelocityQuery(Guid ProjectId, int Weeks)
    : IQuery<IReadOnlyList<VelocityPointDto>>;

public interface IGetProjectVelocityQueryService
{
    Task<IReadOnlyList<VelocityPointDto>> ExecuteAsync(
        Guid projectId,
        int weeks,
        CancellationToken ct
    );
}

internal sealed class GetProjectVelocityHandler(
    IGetProjectVelocityQueryService svc,
    IProjectRepository projects
) : IQueryHandler<GetProjectVelocityQuery, IReadOnlyList<VelocityPointDto>>
{
    private const int MaxWeeks = 52;
    private const int DefaultWeeks = 12;

    private readonly IGetProjectVelocityQueryService _svc = svc;
    private readonly IProjectRepository _projects = projects;

    public async Task<Result<IReadOnlyList<VelocityPointDto>>> Handle(
        GetProjectVelocityQuery request,
        CancellationToken ct
    )
    {
        var project = await _projects.GetByIdAsync(request.ProjectId, ct);
        if (project is null)
            return Error.NotFound("Project not found.");

        var weeks = request.Weeks <= 0 ? DefaultWeeks : Math.Min(request.Weeks, MaxWeeks);
        return Result.Success(await _svc.ExecuteAsync(project.Id, weeks, ct));
    }
}
