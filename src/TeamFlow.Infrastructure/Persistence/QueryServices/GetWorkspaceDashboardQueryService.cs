using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Dashboard.Queries.GetWorkspaceDashboard;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

/// <summary>
/// Aggregates per-user counts for the workspace home page. Each metric is a separate roundtrip
/// — Postgres covers each with an indexed scan and the total latency stays well under a single
/// joined-query alternative that would otherwise force cross-table fan-out.
/// </summary>
internal sealed class GetWorkspaceDashboardQueryService : IGetWorkspaceDashboardQueryService
{
    private const int DueSoonWindowDays = 7;

    private readonly TeamFlowDbContext _ctx;

    public GetWorkspaceDashboardQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    public async Task<WorkspaceDashboardDto> ExecuteAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken ct
    )
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dueSoonCutoff = today.AddDays(DueSoonWindowDays);

        // Tasks assigned to the caller in projects of this workspace, excluding Done.
        var assignedOpenQuery = _ctx
            .Tasks.AsNoTracking()
            .Where(t =>
                t.AssigneeId == userId
                && t.Column != TaskColumn.Done
                && t.DeletedAt == null
                && _ctx.Projects.Any(p => p.Id == t.ProjectId && p.WorkspaceId == workspaceId)
            );

        var openTasks = await assignedOpenQuery.CountAsync(ct);
        var overdueTasks = await assignedOpenQuery
            .Where(t => t.DueDate != null && t.DueDate < today)
            .CountAsync(ct);
        var dueSoonTasks = await assignedOpenQuery
            .Where(t => t.DueDate != null && t.DueDate >= today && t.DueDate <= dueSoonCutoff)
            .CountAsync(ct);

        var unreadNotifications = await _ctx
            .Notifications.AsNoTracking()
            .Where(n => n.RecipientId == userId && n.WorkspaceId == workspaceId && n.ReadAt == null)
            .CountAsync(ct);

        // A channel is "unread" if it contains at least one message authored after the caller's
        // last-read marker. Restricted to channels the caller is a member of in this workspace.
        var unreadChannels = await _ctx
            .ChannelMembers.AsNoTracking()
            .Where(cm => cm.UserId == userId)
            .Where(cm =>
                _ctx.Channels.Any(c => c.Id == cm.ChannelId && c.WorkspaceId == workspaceId)
            )
            .Where(cm =>
                _ctx.Messages.Any(m =>
                    m.ChannelId == cm.ChannelId
                    && m.AuthorId != userId
                    && m.DeletedAt == null
                    && m.CreatedAt > cm.LastReadAt
                )
            )
            .CountAsync(ct);

        var myProjects = await _ctx
            .ProjectMembers.AsNoTracking()
            .Where(pm => pm.UserId == userId)
            .Where(pm =>
                _ctx.Projects.Any(p => p.Id == pm.ProjectId && p.WorkspaceId == workspaceId)
            )
            .CountAsync(ct);

        return new WorkspaceDashboardDto(
            workspaceId,
            openTasks,
            overdueTasks,
            dueSoonTasks,
            unreadNotifications,
            unreadChannels,
            myProjects
        );
    }
}
