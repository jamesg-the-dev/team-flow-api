using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Workspaces.DTOs;
using TeamFlow.Application.Features.Workspaces.Queries.ListWorkspaceMembers;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

internal sealed class ListWorkspaceMembersQueryService : IListWorkspaceMembersQueryService
{
    private readonly TeamFlowDbContext _ctx;

    public ListWorkspaceMembersQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<WorkspaceMemberDto>> ExecuteAsync(
        Guid workspaceId,
        CancellationToken ct
    ) =>
        await _ctx
            .WorkspaceMembers.AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId)
            .OrderBy(m => m.JoinedAt)
            .Select(m => new WorkspaceMemberDto(
                m.UserId,
                m.Role,
                m.Title,
                m.JoinedAt,
                m.InvitedBy,
                _ctx.Profiles.Where(p => p.UserId == m.UserId)
                    .Select(p => p.FullName)
                    .FirstOrDefault()
            ))
            .ToListAsync(ct);
}
