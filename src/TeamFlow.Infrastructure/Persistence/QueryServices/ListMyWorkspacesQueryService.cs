using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Me.DTOs;
using TeamFlow.Application.Features.Me.Queries.ListMyWorkspaces;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

internal sealed class ListMyWorkspacesQueryService : IListMyWorkspacesQueryService
{
    private readonly TeamFlowDbContext _ctx;

    public ListMyWorkspacesQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<MyWorkspaceDto>> ExecuteAsync(
        Guid userId,
        CancellationToken ct
    ) =>
        await _ctx
            .WorkspaceMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(
                _ctx.Workspaces.AsNoTracking(),
                m => m.WorkspaceId,
                w => w.Id,
                (m, w) => new MyWorkspaceDto(w.Id, w.Slug, w.Name, w.LogoUrl, m.Role, m.JoinedAt)
            )
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
}
