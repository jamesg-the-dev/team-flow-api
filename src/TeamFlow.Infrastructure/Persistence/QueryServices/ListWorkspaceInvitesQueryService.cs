using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Features.Workspaces.DTOs;
using TeamFlow.Application.Features.Workspaces.Queries.ListWorkspaceInvites;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

internal sealed class ListWorkspaceInvitesQueryService : IListWorkspaceInvitesQueryService
{
    private readonly TeamFlowDbContext _ctx;
    private readonly IDateTimeProvider _clock;

    public ListWorkspaceInvitesQueryService(TeamFlowDbContext ctx, IDateTimeProvider clock)
    {
        _ctx = ctx;
        _clock = clock;
    }

    public async Task<IReadOnlyList<WorkspaceInviteDto>> ExecuteAsync(
        Guid workspaceId,
        bool includeExpired,
        CancellationToken ct
    )
    {
        var now = _clock.UtcNow;
        var q = _ctx.WorkspaceInvites.AsNoTracking().Where(i => i.WorkspaceId == workspaceId);
        if (!includeExpired)
            q = q.Where(i => i.AcceptedAt == null && i.ExpiresAt > now);

        return await q.OrderByDescending(i => i.CreatedAt)
            .Select(i => new WorkspaceInviteDto(
                i.Id,
                i.Email,
                i.Role,
                i.InvitedBy,
                i.ExpiresAt,
                i.AcceptedAt,
                i.CreatedAt
            ))
            .ToListAsync(ct);
    }
}
