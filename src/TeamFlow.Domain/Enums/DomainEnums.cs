namespace TeamFlow.Domain.Enums;

public enum WorkspaceRole
{
    Owner,
    Admin,
    Member,
    Guest,
}

public enum ProjectStatus
{
    Planning,
    Active,
    OnHold,
    Archived,
    Completed,
}

public enum ProjectMemberRole
{
    Lead,
    Contributor,
    Viewer,
}

public enum PriorityLevel
{
    Low,
    Medium,
    High,
    Critical,
}

public enum TaskColumn
{
    Backlog,
    Todo,
    InProgress,
    Review,
    Done,
}

public enum ChannelType
{
    Public,
    Private,
    Direct,
}

public enum NotificationKind
{
    Mention,
    Assignment,
    Comment,
    Status,
    Invite,
    System,
}

public enum DeliveryChannel
{
    Email,
    Push,
    InApp,
}

public enum AttachmentOwner
{
    Task,
    Message,
    Project,
    Comment,
}
