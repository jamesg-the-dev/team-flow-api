using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Channels.Services;

namespace TeamFlow.Infrastructure.Persistence.Services;

internal sealed class MessagePurger : IMessagePurger
{
    private readonly TeamFlowDbContext _ctx;

    public MessagePurger(TeamFlowDbContext ctx) => _ctx = ctx;

    /// <summary>
    /// Bulk-deletes all messages in a channel, bypassing the global soft-delete query filter so
    /// previously-deleted rows are also purged. Issued as a single SQL DELETE statement.
    /// </summary>
    public Task PurgeMessagesForChannelAsync(Guid channelId, CancellationToken ct) =>
        _ctx
            .Messages.IgnoreQueryFilters()
            .Where(m => m.ChannelId == channelId)
            .ExecuteDeleteAsync(ct);
}
