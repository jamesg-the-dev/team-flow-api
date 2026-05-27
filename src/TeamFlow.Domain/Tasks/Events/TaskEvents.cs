using TeamFlow.Domain.Enums;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Tasks.Events;

public sealed record TaskCreated(Guid TaskId, Guid ProjectId, int Number, Guid ReporterId)
    : DomainEvent;

public sealed record TaskColumnChanged(Guid TaskId, TaskColumn Column, DateTimeOffset At)
    : DomainEvent;

public sealed record TaskAssigneeChanged(Guid TaskId, Guid? AssigneeId) : DomainEvent;

public sealed record TaskCommentAdded(Guid TaskId, Guid CommentId, Guid AuthorId) : DomainEvent;
