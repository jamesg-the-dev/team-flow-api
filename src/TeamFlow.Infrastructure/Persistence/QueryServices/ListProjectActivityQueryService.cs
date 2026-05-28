using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Pagination;
using TeamFlow.Application.Features.Projects.DTOs;
using TeamFlow.Application.Features.Projects.Queries.ListProjectActivity;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

internal sealed class ListProjectActivityQueryService : IListProjectActivityQueryService
{
    private readonly TeamFlowDbContext _ctx;

    public ListProjectActivityQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    public async Task<PagedResult<ProjectActivityDto>> ExecuteAsync(
        Guid projectId,
        PaginationRequest pagination,
        CancellationToken ct
    )
    {
        var baseQuery = _ctx
            .ActivityEvents.AsNoTracking()
            .Where(e => e.ProjectId == projectId);

        var total = await baseQuery.LongCountAsync(ct);
        if (total == 0)
            return PagedResult<ProjectActivityDto>.Empty(pagination);

        var rows = await baseQuery
            .OrderByDescending(e => e.Id)
            .Skip(pagination.Skip)
            .Take(pagination.SafePageSize)
            .Select(e => new
            {
                e.Id,
                e.ActorId,
                e.Verb,
                e.TargetKind,
                e.TargetId,
                MetadataJson = e.Metadata.RootElement.GetRawText(),
                e.CreatedAt,
            })
            .ToListAsync(ct);

        var items = rows
            .Select(r => new ProjectActivityDto(
                r.Id,
                r.ActorId,
                r.Verb,
                r.TargetKind,
                r.TargetId,
                System.Text.Json.JsonDocument.Parse(r.MetadataJson).RootElement,
                r.CreatedAt
            ))
            .ToList();

        return new PagedResult<ProjectActivityDto>(
            items,
            pagination.SafePage,
            pagination.SafePageSize,
            total
        );
    }
}
