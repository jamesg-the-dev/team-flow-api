using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Projects.Events;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Projects;

/// <summary>
/// Project aggregate root. Owns membership, project-tag links, status &amp; schedule.
/// Tasks are a separate aggregate (referenced by ProjectId) to keep the boundary small.
/// </summary>
public sealed class Project : AuditableAggregateRoot, ISoftDeletable
{
    private readonly List<ProjectMember> _members = new();
    private readonly List<ProjectTag> _tags = new();

    public Guid WorkspaceId { get; private set; }
    public string Key { get; private set; } = null!;             // PRJ, used as task prefix
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public ProjectStatus Status { get; private set; } = ProjectStatus.Planning;
    public PriorityLevel Priority { get; private set; } = PriorityLevel.Medium;
    public DateOnly? StartDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public Money? Budget { get; private set; }
    public string? ColorHex { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>Per-project monotonic task numbering (PRJ-1, PRJ-2…). Incremented atomically by infra.</summary>
    public int NextTaskNumber { get; private set; } = 1;

    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();
    public IReadOnlyCollection<ProjectTag> Tags => _tags.AsReadOnly();

    private Project() { }

    public static Project Create(Guid workspaceId, string key, string name, Guid createdBy,
        string? description = null, PriorityLevel priority = PriorityLevel.Medium,
        DateOnly? startDate = null, DateOnly? dueDate = null, string? colorHex = null)
    {
        if (workspaceId == Guid.Empty) throw DomainException.Invariant("WorkspaceId is required.");
        if (string.IsNullOrWhiteSpace(key)) throw DomainException.Invariant("Project key is required.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(key, "^[A-Z][A-Z0-9]{1,9}$"))
            throw DomainException.Invariant("Project key must be 2-10 uppercase alphanumerics starting with a letter.");
        if (string.IsNullOrWhiteSpace(name)) throw DomainException.Invariant("Project name is required.");
        if (startDate is not null && dueDate is not null && dueDate < startDate)
            throw DomainException.Invariant("Due date cannot precede start date.");

        var project = new Project
        {
            Id = Guid.CreateVersion7(),
            WorkspaceId = workspaceId,
            Key = key,
            Name = name.Trim(),
            Description = description?.Trim(),
            Priority = priority,
            StartDate = startDate,
            DueDate = dueDate,
            ColorHex = colorHex,
        };
        // Creator is project lead
        project._members.Add(new ProjectMember(project.Id, createdBy, ProjectMemberRole.Lead, DateTimeOffset.UtcNow));
        project.Raise(new ProjectCreated(project.Id, workspaceId, key, createdBy));
        return project;
    }

    public void UpdateDetails(string name, string? description, PriorityLevel priority,
        DateOnly? startDate, DateOnly? dueDate, string? colorHex)
    {
        if (string.IsNullOrWhiteSpace(name)) throw DomainException.Invariant("Name required.");
        if (startDate is not null && dueDate is not null && dueDate < startDate)
            throw DomainException.Invariant("Due date cannot precede start date.");
        Name = name.Trim();
        Description = description?.Trim();
        Priority = priority;
        StartDate = startDate;
        DueDate = dueDate;
        ColorHex = colorHex;
    }

    public void SetBudget(long? amountCents, string currency = "USD")
    {
        Budget = amountCents.HasValue ? Money.From(amountCents.Value, currency) : null;
    }

    public void ChangeStatus(ProjectStatus next)
    {
        if (Status == next) return;
        // Forbid transition out of Completed → reopen via dedicated method only.
        if (Status == ProjectStatus.Completed && next != ProjectStatus.Active)
            throw DomainException.Invariant("A completed project can only be reactivated.");
        Status = next;
        Raise(new ProjectStatusChanged(Id, next));
    }

    public void Archive()
    {
        if (Status == ProjectStatus.Archived) return;
        Status = ProjectStatus.Archived;
        Raise(new ProjectStatusChanged(Id, Status));
    }

    public ProjectMember AddMember(Guid userId, ProjectMemberRole role)
    {
        if (_members.Any(m => m.UserId == userId))
            throw DomainException.Invariant("User is already a project member.");
        var member = new ProjectMember(Id, userId, role, DateTimeOffset.UtcNow);
        _members.Add(member);
        Raise(new ProjectMemberAdded(Id, userId, role));
        return member;
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId)
                     ?? throw DomainException.NotFound(nameof(ProjectMember), userId);
        _members.Remove(member);
        Raise(new ProjectMemberRemoved(Id, userId));
    }

    public void AddTag(Guid tagId)
    {
        if (_tags.Any(t => t.TagId == tagId)) return;
        _tags.Add(new ProjectTag(Id, tagId));
    }

    public void RemoveTag(Guid tagId) => _tags.RemoveAll(t => t.TagId == tagId);

    /// <summary>Atomically allocate the next task number for this project.</summary>
    public int AllocateTaskNumber() => NextTaskNumber++;

    public void SoftDelete(DateTimeOffset at) => DeletedAt = at;
}
