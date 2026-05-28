using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Pagination;
using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Application.Features.Tasks.Queries.ListProjectTasks;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

internal sealed class ListProjectTasksQueryService : IListProjectTasksQueryService
{
    private readonly TeamFlowDbContext _ctx;

    public ListProjectTasksQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    public async Task<PagedResult<TaskDto>> ExecuteAsync(
        ListProjectTasksQuery query,
        CancellationToken ct
    )
    {
        var q = _ctx.Tasks.AsNoTracking().Where(t => t.ProjectId == query.ProjectId);

        if (query.Column is not null)
            q = q.Where(t => t.Column == query.Column);
        if (query.AssigneeId is not null)
            q = q.Where(t => t.AssigneeId == query.AssigneeId);
        if (query.Priority is not null)
            q = q.Where(t => t.Priority == query.Priority);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = $"%{query.Search.Trim()}%";
            q = q.Where(t => EF.Functions.ILike(t.Title, s)
                || (t.Description != null && EF.Functions.ILike(t.Description, s)));
        }

        var total = await q.LongCountAsync(ct);
        if (total == 0)
            return PagedResult<TaskDto>.Empty(query.Pagination);

        var items = await q
            .OrderBy(t => t.Column)
            .ThenBy(t => t.Position)
            .ThenByDescending(t => t.Number)
            .Skip(query.Pagination.Skip)
            .Take(query.Pagination.SafePageSize)
            .Select(t => new TaskDto(
                t.Id,
                t.ProjectId,
                t.Number,
                t.Title,
                t.Description,
                t.Column,
                t.Priority,
                t.Position,
                t.AssigneeId,
                t.ReporterId,
                t.EstimateHours,
                t.DueDate,
                t.CompletedAt,
                t.CreatedAt,
                t.UpdatedAt
            ))
            .ToListAsync(ct);

        return new PagedResult<TaskDto>(
            items,
            query.Pagination.SafePage,
            query.Pagination.SafePageSize,
            total
        );
    }
}
