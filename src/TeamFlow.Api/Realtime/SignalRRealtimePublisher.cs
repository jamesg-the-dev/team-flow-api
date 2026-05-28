using Microsoft.AspNetCore.SignalR;
using TeamFlow.Application.Common.Realtime;

namespace TeamFlow.Api.Realtime;

/// <summary>
/// SignalR-backed sink that fans realtime events out to the appropriate group
/// (<see cref="RealtimeGroups"/>). Registered as a singleton because <see cref="IHubContext{T,U}"/>
/// is safe to capture for the lifetime of the host.
/// </summary>
internal sealed class SignalRRealtimePublisher : IRealtimePublisher
{
    private readonly IHubContext<RealtimeHub, IRealtimeClient> _hub;
    private readonly ILogger<SignalRRealtimePublisher> _logger;

    public SignalRRealtimePublisher(
        IHubContext<RealtimeHub, IRealtimeClient> hub,
        ILogger<SignalRRealtimePublisher> logger
    )
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task PublishAsync(IReadOnlyList<RealtimeEvent> events, CancellationToken ct)
    {
        foreach (var evt in events)
        {
            try
            {
                var group = evt.Target switch
                {
                    RealtimeTarget.User => RealtimeGroups.User(evt.TargetId),
                    RealtimeTarget.Channel => RealtimeGroups.Channel(evt.TargetId),
                    RealtimeTarget.Workspace => RealtimeGroups.Workspace(evt.TargetId),
                    _ => null,
                };
                if (group is null)
                    continue;

                await _hub.Clients.Group(group).Event(evt.EventName, evt.Payload);
            }
            catch (Exception ex)
            {
                // Per-event isolation: a single bad payload should not stop the rest.
                _logger.LogWarning(
                    ex,
                    "Failed to publish realtime event {EventName} to {Target} {TargetId}",
                    evt.EventName,
                    evt.Target,
                    evt.TargetId
                );
            }
        }
    }
}
