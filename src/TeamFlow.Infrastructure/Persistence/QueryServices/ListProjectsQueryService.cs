using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Pagination;
using TeamFlow.Application.Features.Projects.DTOs;
using TeamFlow.Application.Features.Projects.Queries.ListProjects;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

internal sealed class ListProjectsQueryService : IListProjectsQueryService
{
    private readonly TeamFlowDbContext _ctx;

    public ListProjectsQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    public async Task<PagedResult<ProjectSummaryDto>> ExecuteAsync(
        ListProjectsQuery q,
        CancellationToken ct
    )
    {
        var query = _ctx.Projects.AsNoTracking().Where(p => p.WorkspaceId == q.WorkspaceId);

        if (q.ActiveOnly)
        {
            query = query.Where(p =>
                p.Status != ProjectStatus.Completed
                && p.Status != ProjectStatus.OnHold
                && p.Status != ProjectStatus.Archived
            );
        }

        if (q.Status is { } status)
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var search = $"%{q.Search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, search) || EF.Functions.ILike(p.Key, search)
            );
        }

        var total = await query.LongCountAsync(ct);
        if (total == 0)
            return PagedResult<ProjectSummaryDto>.Empty(q.Pagination);

        var items = await query
            .OrderByDescending(p => p.UpdatedAt)
            .Skip(q.Pagination.Skip)
            .Take(q.Pagination.SafePageSize)
            .Select(p => new ProjectSummaryDto(
                p.Id,
                p.Key,
                p.Name,
                p.Status,
                p.Priority,
                p.DueDate,
                p.Members.Count,
                p.Members
                    .OrderBy(m => m.AddedAt)
                    .Select(m =>
                        _ctx.Profiles
                            .Where(pr => pr.UserId == m.UserId)
                            .Select(pr => pr.FullName)
                            .FirstOrDefault() ?? string.Empty)
                    .ToList()
            ))
            .ToListAsync(ct);

        return new PagedResult<ProjectSummaryDto>(
            items,
            q.Pagination.SafePage,
            q.Pagination.SafePageSize,
            total
        );
    }
}
