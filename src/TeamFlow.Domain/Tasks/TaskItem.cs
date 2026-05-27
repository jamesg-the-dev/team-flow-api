using TeamFlow.Domain.Enums;
using TeamFlow.Domain.SeedWork;
using TeamFlow.Domain.Tasks.Events;

namespace TeamFlow.Domain.Tasks;

/// <summary>
/// Kanban work item. Aggregate root for watchers, dependencies, tag links and comments.
/// Comments are kept inside the aggregate because they share the task's consistency boundary
/// (deleting a task soft-deletes its comments; reply threads are scoped to a single task).
/// </summary>
public sealed class TaskItem : AuditableAggregateRoot, ISoftDeletable
{
    private readonly List<TaskTag> _tags = new();
    private readonly List<TaskWatcher> _watchers = new();
    private readonly List<TaskDependency> _dependencies = new();
    private readonly List<TaskComment> _comments = new();

    public Guid WorkspaceId { get; private set; }
    public Guid ProjectId { get; private set; }
    public int Number { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public TaskColumn Column { get; private set; } = TaskColumn.Backlog;
    public PriorityLevel Priority { get; private set; } = PriorityLevel.Medium;
    public decimal Position { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public Guid ReporterId { get; private set; }
    public decimal? EstimateHours { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public IReadOnlyCollection<TaskTag> Tags => _tags.AsReadOnly();
    public IReadOnlyCollection<TaskWatcher> Watchers => _watchers.AsReadOnly();
    public IReadOnlyCollection<TaskDependency> Dependencies => _dependencies.AsReadOnly();
    public IReadOnlyCollection<TaskComment> Comments => _comments.AsReadOnly();

    private TaskItem() { }

    public static TaskItem Create(
        Guid workspaceId,
        Guid projectId,
        int number,
        string title,
        Guid reporterId,
        decimal position,
        string? description = null,
        PriorityLevel priority = PriorityLevel.Medium,
        Guid? assigneeId = null,
        decimal? estimateHours = null,
        DateOnly? dueDate = null
    )
    {
        if (string.IsNullOrWhiteSpace(title))
            throw DomainException.Invariant("Title is required.");
        if (title.Length > 300)
            throw DomainException.Invariant("Title cannot exceed 300 characters.");
        if (number <= 0)
            throw DomainException.Invariant("Task number must be positive.");
        if (estimateHours is < 0)
            throw DomainException.Invariant("Estimate cannot be negative.");

        var task = new TaskItem
        {
            Id = Guid.CreateVersion7(),
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            Number = number,
            Title = title.Trim(),
            Description = description?.Trim(),
            Priority = priority,
            Position = position,
            AssigneeId = assigneeId,
            ReporterId = reporterId,
            EstimateHours = estimateHours,
            DueDate = dueDate,
        };
        task.Raise(new TaskCreated(task.Id, projectId, number, reporterId));
        return task;
    }

    public void UpdateDetails(
        string title,
        string? description,
        PriorityLevel priority,
        decimal? estimateHours,
        DateOnly? dueDate
    )
    {
        if (string.IsNullOrWhiteSpace(title))
            throw DomainException.Invariant("Title is required.");
        if (estimateHours is < 0)
            throw DomainException.Invariant("Estimate cannot be negative.");
        Title = title.Trim();
        Description = description?.Trim();
        Priority = priority;
        EstimateHours = estimateHours;
        DueDate = dueDate;
    }

    /// <summary>Move to a different column and/or position. Done transition records completion.</summary>
    public void MoveTo(TaskColumn column, decimal newPosition, DateTimeOffset now)
    {
        var changedColumn = column != Column;
        Column = column;
        Position = newPosition;

        if (changedColumn)
        {
            if (column == TaskColumn.Done && CompletedAt is null)
                CompletedAt = now;
            else if (column != TaskColumn.Done)
                CompletedAt = null;
            Raise(new TaskColumnChanged(Id, column, now));
        }
    }

    public void Assign(Guid? assigneeId)
    {
        if (AssigneeId == assigneeId)
            return;
        AssigneeId = assigneeId;
        Raise(new TaskAssigneeChanged(Id, assigneeId));
    }

    public void AddTag(Guid tagId)
    {
        if (_tags.Any(t => t.TagId == tagId))
            return;
        _tags.Add(new TaskTag(Id, tagId));
    }

    public void RemoveTag(Guid tagId) => _tags.RemoveAll(t => t.TagId == tagId);

    public void AddWatcher(Guid userId)
    {
        if (_watchers.Any(w => w.UserId == userId))
            return;
        _watchers.Add(new TaskWatcher(Id, userId));
    }

    public void RemoveWatcher(Guid userId) => _watchers.RemoveAll(w => w.UserId == userId);

    public void AddDependency(Guid dependsOnId)
    {
        if (dependsOnId == Id)
            throw DomainException.Invariant("A task cannot depend on itself.");
        if (_dependencies.Any(d => d.DependsOnId == dependsOnId))
            return;
        _dependencies.Add(new TaskDependency(Id, dependsOnId));
    }

    public void RemoveDependency(Guid dependsOnId) =>
        _dependencies.RemoveAll(d => d.DependsOnId == dependsOnId);

    public TaskComment AddComment(Guid authorId, string body, Guid? parentId = null)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw DomainException.Invariant("Comment body is required.");
        if (parentId is not null && _comments.All(c => c.Id != parentId))
            throw DomainException.Invariant("Parent comment does not belong to this task.");
        var comment = new TaskComment(Id, authorId, body.Trim(), parentId);
        _comments.Add(comment);
        Raise(new TaskCommentAdded(Id, comment.Id, authorId));
        return comment;
    }

    public void EditComment(Guid commentId, Guid editorId, string body, DateTimeOffset now)
    {
        var comment =
            _comments.FirstOrDefault(c => c.Id == commentId)
            ?? throw DomainException.NotFound(nameof(TaskComment), commentId);
        comment.Edit(editorId, body, now);
    }

    public void DeleteComment(Guid commentId, DateTimeOffset now)
    {
        var comment =
            _comments.FirstOrDefault(c => c.Id == commentId)
            ?? throw DomainException.NotFound(nameof(TaskComment), commentId);
        comment.SoftDelete(now);
    }

    public void SoftDelete(DateTimeOffset at) => DeletedAt = at;
}
