using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Projects.DTOs;

public sealed record ProjectDto(
    Guid Id,
    Guid WorkspaceId,
    string Key,
    string Name,
    string? Description,
    ProjectStatus Status,
    PriorityLevel Priority,
    DateOnly? StartDate,
    DateOnly? DueDate,
    long? BudgetCents,
    string? BudgetCurrency,
    string? ColorHex,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record ProjectSummaryDto(
    Guid Id,
    string Key,
    string Name,
    ProjectStatus Status,
    PriorityLevel Priority,
    DateOnly? DueDate,
    int MemberCount,
    IReadOnlyList<string> MemberNames
);
