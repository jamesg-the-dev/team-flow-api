using TeamFlow.Domain.Enums;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Projects;

public sealed class ProjectMember
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public ProjectMemberRole Role { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }

    private ProjectMember() { }

    internal ProjectMember(
        Guid projectId,
        Guid userId,
        ProjectMemberRole role,
        DateTimeOffset addedAt
    )
    {
        ProjectId = projectId;
        UserId = userId;
        Role = role;
        AddedAt = addedAt;
    }

    internal void ChangeRole(ProjectMemberRole role) => Role = role;
}

public sealed class ProjectTag
{
    public Guid ProjectId { get; private set; }
    public Guid TagId { get; private set; }

    private ProjectTag() { }

    internal ProjectTag(Guid projectId, Guid tagId)
    {
        ProjectId = projectId;
        TagId = tagId;
    }
}
