using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Tasks;

public sealed class TaskTag
{
    public Guid TaskId { get; private set; }
    public Guid TagId { get; private set; }
    private TaskTag() { }
    internal TaskTag(Guid taskId, Guid tagId) { TaskId = taskId; TagId = tagId; }
}

public sealed class TaskWatcher
{
    public Guid TaskId { get; private set; }
    public Guid UserId { get; private set; }
    private TaskWatcher() { }
    internal TaskWatcher(Guid taskId, Guid userId) { TaskId = taskId; UserId = userId; }
}

public sealed class TaskDependency
{
    public Guid TaskId { get; private set; }
    public Guid DependsOnId { get; private set; }
    private TaskDependency() { }
    internal TaskDependency(Guid taskId, Guid dependsOnId) { TaskId = taskId; DependsOnId = dependsOnId; }
}

/// <summary>Threaded comment local to a TaskItem aggregate.</summary>
public sealed class TaskComment : Entity, ISoftDeletable
{
    public Guid TaskId { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Body { get; private set; } = null!;
    public DateTimeOffset? EditedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private TaskComment() { }

    internal TaskComment(Guid taskId, Guid authorId, string body, Guid? parentId)
        : base(Guid.CreateVersion7())
    {
        TaskId = taskId;
        AuthorId = authorId;
        Body = body;
        ParentId = parentId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    internal void Edit(Guid editorId, string body, DateTimeOffset now)
    {
        if (editorId != AuthorId) throw DomainException.Invariant("Only the author can edit a comment.");
        if (string.IsNullOrWhiteSpace(body)) throw DomainException.Invariant("Body required.");
        Body = body.Trim();
        EditedAt = now;
    }

    public void SoftDelete(DateTimeOffset at) => DeletedAt = at;
}
