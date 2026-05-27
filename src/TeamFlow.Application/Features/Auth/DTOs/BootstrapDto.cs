namespace TeamFlow.Application.Features.Auth.DTOs;

/// <summary>
/// Result of <c>POST /auth/bootstrap</c>. Returns the user's default workspace and a flag
/// indicating whether one was just provisioned for them.
/// </summary>
public sealed record BootstrapDto(
    Guid WorkspaceId,
    string WorkspaceSlug,
    string WorkspaceName,
    bool WorkspaceCreated
);
