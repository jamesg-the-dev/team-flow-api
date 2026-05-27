using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Workspaces.Commands.UpdateWorkspace;

/// <summary>Updates the mutable, non-billing properties of a workspace.</summary>
public sealed record UpdateWorkspaceCommand(Guid WorkspaceId, string Name, string? LogoUrl)
    : ICommand;

public sealed class UpdateWorkspaceValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    public UpdateWorkspaceValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LogoUrl).MaximumLength(2000);
    }
}

internal sealed class UpdateWorkspaceHandler : ICommandHandler<UpdateWorkspaceCommand>
{
    private readonly IWorkspaceRepository _repository;
    private readonly ICurrentUser _currentUser;

    public UpdateWorkspaceHandler(IWorkspaceRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateWorkspaceCommand request, CancellationToken ct)
    {
        var workspace = await _repository.GetByIdAsync(request.WorkspaceId, ct);
        if (workspace is null)
            return Error.NotFound("Workspace not found.");

        var userId = _currentUser.RequireUserId();
        if (!workspace.Members.Any(m =>
                m.UserId == userId
                && (m.Role == Domain.Enums.WorkspaceRole.Owner
                    || m.Role == Domain.Enums.WorkspaceRole.Admin)))
            return Error.Forbidden("Only workspace owners or admins can update the workspace.");

        workspace.Rename(request.Name);
        workspace.SetLogo(request.LogoUrl);

        return Result.Success();
    }
}
