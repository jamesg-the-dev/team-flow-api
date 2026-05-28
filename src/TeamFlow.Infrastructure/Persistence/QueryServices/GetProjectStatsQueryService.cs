using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Features.Projects.DTOs;
using TeamFlow.Application.Features.Projects.Queries.GetProjectStats;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

internal sealed class GetProjectStatsQueryService : IGetProjectStatsQueryService
{
    private readonly TeamFlowDbContext _ctx;
    private readonly IDateTimeProvider _clock;

    public GetProjectStatsQueryService(TeamFlowDbContext ctx, IDateTimeProvider clock)
    {
        _ctx = ctx;
        _clock = clock;
    }

    public async Task<ProjectStatsDto> ExecuteAsync(
        Guid projectId,
        CancellationToken ct
    )
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);

        var tasks = _ctx.Tasks.AsNoTracking().Where(t => t.ProjectId == projectId);

        var byColumnRaw = await tasks
            .GroupBy(t => t.Column)
            .Select(g => new { Column = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var byPriorityRaw = await tasks
            .GroupBy(t => t.Priority)
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var total = byColumnRaw.Sum(x => x.Count);
        var closed = byColumnRaw
            .Where(x => x.Column == TaskColumn.Done)
            .Sum(x => x.Count);
        var open = total - closed;

        var overdue = await tasks
            .Where(t =>
                t.DueDate != null
                && t.DueDate < today
                && t.CompletedAt == null
                && t.Column != TaskColumn.Done
            )
            .CountAsync(ct);

        var workload = await tasks
            .Where(t => t.CompletedAt == null && t.Column != TaskColumn.Done)
            .GroupBy(t => t.AssigneeId)
            .Select(g => new MemberWorkloadDto(
                g.Key,
                g.Count(),
                g.Count(t => t.DueDate != null && t.DueDate < today)
            ))
            .ToListAsync(ct);

        return new ProjectStatsDto(
            total,
            open,
            closed,
            overdue,
            byColumnRaw.ToDictionary(x => x.Column.ToString(), x => x.Count),
            byPriorityRaw.ToDictionary(x => x.Priority.ToString(), x => x.Count),
            workload
        );
    }
}
