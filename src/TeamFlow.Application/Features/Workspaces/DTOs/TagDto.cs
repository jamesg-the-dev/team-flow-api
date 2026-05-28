namespace TeamFlow.Application.Features.Workspaces.DTOs;

public sealed record TagDto(Guid Id, Guid WorkspaceId, string Name, string ColorHex);
