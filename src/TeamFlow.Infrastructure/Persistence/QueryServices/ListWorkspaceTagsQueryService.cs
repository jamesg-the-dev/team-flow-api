using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Workspaces.DTOs;
using TeamFlow.Application.Features.Workspaces.Queries.ListWorkspaceTags;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

internal sealed class ListWorkspaceTagsQueryService : IListWorkspaceTagsQueryService
{
    private readonly TeamFlowDbContext _ctx;

    public ListWorkspaceTagsQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<TagDto>> ExecuteAsync(
        Guid workspaceId,
        CancellationToken ct
    ) =>
        await _ctx
            .Tags.AsNoTracking()
            .Where(t => t.WorkspaceId == workspaceId)
            .OrderBy(t => t.Name)
            .Select(t => new TagDto(t.Id, t.WorkspaceId, t.Name, t.ColorHex))
            .ToListAsync(ct);
}
