using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Tasks.DTOs;

public sealed record TaskDto(
    Guid Id,
    Guid ProjectId,
    int Number,
    string Title,
    string? Description,
    TaskColumn Column,
    PriorityLevel Priority,
    decimal Position,
    Guid? AssigneeId,
    Guid ReporterId,
    decimal? EstimateHours,
    DateOnly? DueDate,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record TaskBoardCardDto(
    Guid Id,
    int Number,
    string Title,
    TaskColumn Column,
    PriorityLevel Priority,
    decimal Position,
    Guid? AssigneeId,
    DateOnly? DueDate);

public sealed record TaskCommentDto(
    Guid Id,
    Guid TaskId,
    Guid AuthorId,
    Guid? ParentId,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt);
