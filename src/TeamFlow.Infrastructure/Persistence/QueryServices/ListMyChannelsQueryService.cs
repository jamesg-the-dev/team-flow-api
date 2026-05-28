using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Channels.DTOs;
using TeamFlow.Application.Features.Channels.Queries.ListMyChannels;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

internal sealed class ListMyChannelsQueryService : IListMyChannelsQueryService
{
    private readonly TeamFlowDbContext _ctx;

    public ListMyChannelsQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    /// <summary>
    /// Projects every channel the user is a member of in the given workspace, along with the
    /// number of messages newer than their <c>LastReadAt</c> marker and the timestamp of the
    /// latest message. The unread count is computed via a correlated sub-query so the database
    /// can use the (channel_id, created_at) index.
    /// </summary>
    public async Task<IReadOnlyList<MyChannelDto>> ListAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken ct
    )
    {
        var rows = await (
            from cm in _ctx.ChannelMembers.AsNoTracking()
            join c in _ctx.Channels.AsNoTracking() on cm.ChannelId equals c.Id
            where cm.UserId == userId && c.WorkspaceId == workspaceId
            select new
            {
                c.Id,
                c.Name,
                c.Topic,
                c.Type,
                c.CreatedAt,
                cm.LastReadAt,
                cm.IsMuted,
                UnreadCount = _ctx.Messages.Count(m =>
                    m.ChannelId == c.Id && m.CreatedAt > cm.LastReadAt
                ),
                LastMessageAt = _ctx.Messages
                    .Where(m => m.ChannelId == c.Id)
                    .Max(m => (DateTimeOffset?)m.CreatedAt),
            }
        )
            .ToListAsync(ct);

        return rows
            .OrderByDescending(r => r.LastMessageAt ?? r.CreatedAt)
            .Select(r => new MyChannelDto(
                r.Id,
                r.Name,
                r.Topic,
                r.Type,
                r.CreatedAt,
                r.LastReadAt,
                r.IsMuted,
                r.UnreadCount,
                r.LastMessageAt
            ))
            .ToList();
    }
}
