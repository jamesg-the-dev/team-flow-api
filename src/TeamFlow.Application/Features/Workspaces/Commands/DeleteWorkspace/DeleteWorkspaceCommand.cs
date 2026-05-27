using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Workspaces.Commands.DeleteWorkspace;

/// <summary>Soft-deletes a workspace. Only the owner may do this.</summary>
public sealed record DeleteWorkspaceCommand(Guid WorkspaceId) : ICommand;

internal sealed class DeleteWorkspaceHandler : ICommandHandler<DeleteWorkspaceCommand>
{
    private readonly IWorkspaceRepository _repository;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _currentUser;

    public DeleteWorkspaceHandler(
        IWorkspaceRepository repository,
        IDateTimeProvider clock,
        ICurrentUser currentUser
    )
    {
        _repository = repository;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteWorkspaceCommand request, CancellationToken ct)
    {
        var workspace = await _repository.GetByIdAsync(request.WorkspaceId, ct);
        if (workspace is null)
            return Error.NotFound("Workspace not found.");

        if (workspace.OwnerId != _currentUser.RequireUserId())
            return Error.Forbidden("Only the workspace owner can delete this workspace.");

        workspace.SoftDelete(_clock.UtcNow);
        return Result.Success();
    }
}
