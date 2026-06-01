using System.Text.Json;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Projects.DTOs;

public sealed record ProjectMemberDto(
    Guid UserId,
    ProjectMemberRole Role,
    string FullName,
    DateTimeOffset AddedAt
);

public sealed record ProjectActivityDto(
    long Id,
    Guid? ActorId,
    string Verb,
    string TargetKind,
    Guid TargetId,
    JsonElement Metadata,
    DateTimeOffset CreatedAt
);

public sealed record ProjectStatsDto(
    int TotalTasks,
    int OpenTasks,
    int ClosedTasks,
    int OverdueTasks,
    IReadOnlyDictionary<string, int> ByColumn,
    IReadOnlyDictionary<string, int> ByPriority,
    IReadOnlyList<MemberWorkloadDto> ByAssignee
);

public sealed record MemberWorkloadDto(Guid? UserId, int Open, int Overdue);
