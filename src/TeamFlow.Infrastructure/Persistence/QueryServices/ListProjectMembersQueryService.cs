using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Projects.DTOs;
using TeamFlow.Application.Features.Projects.Queries.ListProjectMembers;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

internal sealed class ListProjectMembersQueryService : IListProjectMembersQueryService
{
    private readonly TeamFlowDbContext _ctx;

    public ListProjectMembersQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<ProjectMemberDto>> ExecuteAsync(
        Guid projectId,
        CancellationToken ct
    ) =>
        await _ctx
            .ProjectMembers.AsNoTracking()
            .Where(m => m.ProjectId == projectId)
            .OrderBy(m => m.AddedAt)
            .Select(m => new ProjectMemberDto(m.UserId, m.Role, m.AddedAt))
            .ToListAsync(ct);
}
