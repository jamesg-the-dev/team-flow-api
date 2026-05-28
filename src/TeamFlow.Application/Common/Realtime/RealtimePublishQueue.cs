namespace TeamFlow.Application.Common.Realtime;

internal sealed class RealtimePublishQueue : IRealtimePublishQueue
{
    private readonly List<RealtimeEvent> _events = new();

    public void Enqueue(RealtimeEvent evt) => _events.Add(evt);

    public IReadOnlyList<RealtimeEvent> Drain()
    {
        if (_events.Count == 0)
            return Array.Empty<RealtimeEvent>();
        var snapshot = _events.ToArray();
        _events.Clear();
        return snapshot;
    }
}

/// <summary>
/// Default sink used when the host has not registered a transport. Silently drops events so
/// background-only scenarios (workers, integration tests) don't need to spin up SignalR.
/// </summary>
internal sealed class NoOpRealtimePublisher : IRealtimePublisher
{
    public Task PublishAsync(IReadOnlyList<RealtimeEvent> events, CancellationToken ct) =>
        Task.CompletedTask;
}
