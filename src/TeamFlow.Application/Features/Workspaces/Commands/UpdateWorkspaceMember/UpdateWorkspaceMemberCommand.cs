using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Workspaces.Commands.UpdateWorkspaceMember;

/// <summary>Changes role and/or title for a workspace member.</summary>
public sealed record UpdateWorkspaceMemberCommand(
    Guid WorkspaceId,
    Guid UserId,
    WorkspaceRole? Role,
    string? Title,
    bool ClearTitle = false
) : ICommand;

public sealed class UpdateWorkspaceMemberValidator
    : AbstractValidator<UpdateWorkspaceMemberCommand>
{
    public UpdateWorkspaceMemberValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(200);
    }
}

internal sealed class UpdateWorkspaceMemberHandler
    : ICommandHandler<UpdateWorkspaceMemberCommand>
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public UpdateWorkspaceMemberHandler(IWorkspaceRepository workspaces, ICurrentUser currentUser)
    {
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        UpdateWorkspaceMemberCommand request,
        CancellationToken ct
    )
    {
        var workspace = await _workspaces.GetByIdAsync(request.WorkspaceId, ct);
        if (workspace is null)
            return Error.NotFound("Workspace not found.");

        if (!WorkspaceAuthorization.IsOwnerOrAdmin(workspace, _currentUser.RequireUserId()))
            return Error.Forbidden("Only workspace owners or admins can update members.");

        if (request.Role is { } role)
            workspace.ChangeMemberRole(request.UserId, role);

        if (request.ClearTitle)
            workspace.ChangeMemberTitle(request.UserId, null);
        else if (!string.IsNullOrWhiteSpace(request.Title))
            workspace.ChangeMemberTitle(request.UserId, request.Title);

        return Result.Success();
    }
}
