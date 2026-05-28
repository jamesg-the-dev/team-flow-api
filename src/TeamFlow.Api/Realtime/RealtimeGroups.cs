namespace TeamFlow.Api.Realtime;

/// <summary>SignalR group name conventions. Keep symmetric with the client SDK.</summary>
internal static class RealtimeGroups
{
    public static string User(Guid userId) => $"user:{userId}";

    public static string Channel(Guid channelId) => $"channel:{channelId}";

    public static string Workspace(Guid workspaceId) => $"workspace:{workspaceId}";
}
