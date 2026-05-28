namespace TeamFlow.Api.Realtime;

/// <summary>
/// Strongly-typed contract for server→client messages on <see cref="RealtimeHub"/>. The single
/// <c>Event</c> method delivers all event types — the payload's shape is identified by
/// <paramref name="eventName"/> (see <see cref="Application.Common.Realtime.RealtimeEvents"/>).
/// This keeps the wire small and means we don't have to widen the hub every time we add a
/// new event type.
/// </summary>
public interface IRealtimeClient
{
    Task Event(string eventName, object payload);
}
