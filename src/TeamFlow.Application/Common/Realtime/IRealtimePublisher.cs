namespace TeamFlow.Application.Common.Realtime;

/// <summary>
/// Scoped buffer of realtime events captured during the current command's unit of work.
/// Handlers (or downstream services like the notification dispatcher) enqueue events;
/// the UoW pipeline behavior drains the queue through <see cref="IRealtimePublisher"/>
/// after a successful commit. If the commit throws, the buffer is discarded with the scope.
/// </summary>
public interface IRealtimePublishQueue
{
    void Enqueue(RealtimeEvent evt);

    IReadOnlyList<RealtimeEvent> Drain();
}

/// <summary>
/// Transport-facing sink. The Api project supplies a SignalR-backed implementation.
/// Implementations must be safe to call after the originating DbContext has been disposed.
/// </summary>
public interface IRealtimePublisher
{
    Task PublishAsync(IReadOnlyList<RealtimeEvent> events, CancellationToken ct);
}
