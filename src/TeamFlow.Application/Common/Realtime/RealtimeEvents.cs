namespace TeamFlow.Application.Common.Realtime;

/// <summary>Well-known server→client event names. Keep aligned with the client SDK constants.</summary>
public static class RealtimeEvents
{
    // Chat
    public const string MessagePosted = "messagePosted";
    public const string MessageEdited = "messageEdited";
    public const string MessageDeleted = "messageDeleted";
    public const string MessagePinned = "messagePinned";
    public const string ReactionChanged = "reactionChanged";

    // Notifications
    public const string NotificationCreated = "notificationCreated";
}
