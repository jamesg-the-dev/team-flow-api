namespace TeamFlow.Application.Features.Channels.Services;

/// <summary>
/// Bulk-deletes the messages of a channel without materializing them. Implemented in
/// Infrastructure using <c>ExecuteDeleteAsync</c> so that hard-delete of a channel does not
/// require loading every message into memory.
/// </summary>
public interface IMessagePurger
{
    Task PurgeMessagesForChannelAsync(Guid channelId, CancellationToken ct);
}
