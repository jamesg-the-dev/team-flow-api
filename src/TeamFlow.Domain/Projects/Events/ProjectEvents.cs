using TeamFlow.Domain.Enums;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Projects.Events;

public sealed record ProjectCreated(Guid ProjectId, Guid WorkspaceId, string Key, Guid CreatedBy) : DomainEvent;
public sealed record ProjectStatusChanged(Guid ProjectId, ProjectStatus Status) : DomainEvent;
public sealed record ProjectMemberAdded(Guid ProjectId, Guid UserId, ProjectMemberRole Role) : DomainEvent;
public sealed record ProjectMemberRemoved(Guid ProjectId, Guid UserId) : DomainEvent;
