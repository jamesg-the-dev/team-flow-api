using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Messages;
using TeamFlow.Application.Features.Messages.DTOs;
using TeamFlow.Application.Features.Messages.Queries.GetMessage;
using TeamFlow.Application.Features.Messages.Queries.ListChannelMessages;
using TeamFlow.Application.Features.Messages.Queries.ListChannelPins;
using TeamFlow.Application.Features.Messages.Queries.ListThreadMessages;
using TeamFlow.Domain.Discussions;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

/// <summary>
/// Shared helpers for projecting <see cref="Message"/> into <see cref="MessageDto"/> together
/// with each root message's reply count. Replies, reactions and mentions are pulled in with
/// AsSplitQuery to keep result-set cardinality manageable when fanning out children.
/// </summary>
internal static class MessageQueryHelpers
{
    public static async Task<IReadOnlyList<MessageDto>> ProjectAsync(
        IQueryable<Message> source,
        TeamFlowDbContext ctx,
        CancellationToken ct
    )
    {
        var messages = await source
            .AsNoTracking()
            .Include(m => m.Reactions)
            .Include(m => m.Mentions)
            .AsSplitQuery()
            .ToListAsync(ct);

        if (messages.Count == 0)
            return Array.Empty<MessageDto>();

        var rootIds = messages.Where(m => m.ParentId is null).Select(m => m.Id).ToArray();
        var replyCounts = rootIds.Length == 0
            ? new Dictionary<Guid, int>()
            : await ctx
                .Messages.AsNoTracking()
                .Where(m => m.ParentId != null && rootIds.Contains(m.ParentId!.Value))
                .GroupBy(m => m.ParentId!.Value)
                .Select(g => new { ParentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ParentId, x => x.Count, ct);

        return messages
            .Select(m => m.ToDto(replyCounts.GetValueOrDefault(m.Id)))
            .ToList();
    }
}

internal sealed class ListChannelMessagesQueryService : IListChannelMessagesQueryService
{
    private readonly TeamFlowDbContext _ctx;

    public ListChannelMessagesQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<MessageDto>> ExecuteAsync(
        Guid channelId,
        DateTimeOffset? before,
        int take,
        CancellationToken ct
    )
    {
        var q = _ctx
            .Messages.Where(m =>
                m.ChannelId == channelId
                && m.ParentId == null
                && (before == null || m.CreatedAt < before)
            )
            .OrderByDescending(m => m.CreatedAt)
            .Take(take);

        return await MessageQueryHelpers.ProjectAsync(q, _ctx, ct);
    }
}

internal sealed class ListThreadMessagesQueryService : IListThreadMessagesQueryService
{
    private readonly TeamFlowDbContext _ctx;

    public ListThreadMessagesQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<MessageDto>> ExecuteAsync(
        Guid parentMessageId,
        CancellationToken ct
    )
    {
        // Root + replies, chronologically.
        var q = _ctx
            .Messages.Where(m => m.Id == parentMessageId || m.ParentId == parentMessageId)
            .OrderBy(m => m.CreatedAt);

        return await MessageQueryHelpers.ProjectAsync(q, _ctx, ct);
    }
}

internal sealed class ListChannelPinsQueryService : IListChannelPinsQueryService
{
    private readonly TeamFlowDbContext _ctx;

    public ListChannelPinsQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<MessageDto>> ExecuteAsync(
        Guid channelId,
        CancellationToken ct
    )
    {
        var q = _ctx
            .Messages.Where(m => m.ChannelId == channelId && m.IsPinned)
            .OrderByDescending(m => m.CreatedAt);

        return await MessageQueryHelpers.ProjectAsync(q, _ctx, ct);
    }
}

internal sealed class MessageReplyCountService : IMessageReplyCountService
{
    private readonly TeamFlowDbContext _ctx;

    public MessageReplyCountService(TeamFlowDbContext ctx) => _ctx = ctx;

    public Task<int> CountAsync(Guid parentId, CancellationToken ct) =>
        _ctx.Messages.AsNoTracking().CountAsync(m => m.ParentId == parentId, ct);
}
