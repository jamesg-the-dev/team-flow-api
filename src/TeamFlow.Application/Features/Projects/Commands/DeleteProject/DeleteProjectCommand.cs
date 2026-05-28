using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Projects;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Projects.Commands.DeleteProject;

public sealed record DeleteProjectCommand(Guid ProjectId) : ICommand;

internal sealed class DeleteProjectHandler : ICommandHandler<DeleteProjectCommand>
{
    private readonly IProjectRepository _projects;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public DeleteProjectHandler(
        IProjectRepository projects,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser,
        IDateTimeProvider clock
    )
    {
        _projects = projects;
        _workspaces = workspaces;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(DeleteProjectCommand request, CancellationToken ct)
    {
        var project = await _projects.GetByIdWithMembersAsync(request.ProjectId, ct);
        if (project is null)
            return Error.NotFound("Project not found.");

        var userId = _currentUser.RequireUserId();
        if (
            !ProjectAuthorization.IsLead(project, userId)
            && !await _workspaces.IsOwnerOrAdminAsync(project.WorkspaceId, userId, ct)
        )
            return Error.Forbidden(
                "Only project leads or workspace owners/admins can delete a project."
            );

        project.SoftDelete(_clock.UtcNow);
        return Result.Success();
    }
}
