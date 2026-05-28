using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Pagination;
using TeamFlow.Application.Features.Workspaces.Queries.ListWorkspaceActivity;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

internal sealed class ListWorkspaceActivityQueryService : IListWorkspaceActivityQueryService
{
    private readonly TeamFlowDbContext _ctx;

    public ListWorkspaceActivityQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    public async Task<PagedResult<WorkspaceActivityDto>> ExecuteAsync(
        Guid workspaceId,
        PaginationRequest pagination,
        CancellationToken ct
    )
    {
        var baseQuery = _ctx
            .ActivityEvents.AsNoTracking()
            .Where(e => e.WorkspaceId == workspaceId);

        var total = await baseQuery.LongCountAsync(ct);
        if (total == 0)
            return PagedResult<WorkspaceActivityDto>.Empty(pagination);

        var rows = await baseQuery
            .OrderByDescending(e => e.Id)
            .Skip(pagination.Skip)
            .Take(pagination.SafePageSize)
            .Select(e => new
            {
                e.Id,
                e.ActorId,
                e.ProjectId,
                e.Verb,
                e.TargetKind,
                e.TargetId,
                MetadataJson = e.Metadata.RootElement.GetRawText(),
                e.CreatedAt,
            })
            .ToListAsync(ct);

        var items = rows
            .Select(r => new WorkspaceActivityDto(
                r.Id,
                r.ActorId,
                r.ProjectId,
                r.Verb,
                r.TargetKind,
                r.TargetId,
                System.Text.Json.JsonDocument.Parse(r.MetadataJson).RootElement,
                r.CreatedAt
            ))
            .ToList();

        return new PagedResult<WorkspaceActivityDto>(
            items,
            pagination.SafePage,
            pagination.SafePageSize,
            total
        );
    }
}
