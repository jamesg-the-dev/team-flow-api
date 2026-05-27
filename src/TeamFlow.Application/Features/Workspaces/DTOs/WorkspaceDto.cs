namespace TeamFlow.Application.Features.Workspaces.DTOs;

public sealed record WorkspaceDto(
    Guid Id,
    string Slug,
    string Name,
    string? LogoUrl,
    string Plan,
    Guid OwnerId,
    int MemberCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
