using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Projects;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Projects.Commands.RemoveProjectMember;

public sealed record RemoveProjectMemberCommand(Guid ProjectId, Guid UserId) : ICommand;

internal sealed class RemoveProjectMemberHandler
    : ICommandHandler<RemoveProjectMemberCommand>
{
    private readonly IProjectRepository _projects;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public RemoveProjectMemberHandler(
        IProjectRepository projects,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _projects = projects;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RemoveProjectMemberCommand request, CancellationToken ct)
    {
        var project = await _projects.GetByIdWithMembersAsync(request.ProjectId, ct);
        if (project is null)
            return Error.NotFound("Project not found.");

        var actor = _currentUser.RequireUserId();
        var isSelf = request.UserId == actor;
        if (
            !isSelf
            && !ProjectAuthorization.IsLead(project, actor)
            && !await _workspaces.IsOwnerOrAdminAsync(project.WorkspaceId, actor, ct)
        )
            return Error.Forbidden(
                "Only project leads, workspace owners/admins, or the member themselves can remove a project member."
            );

        project.RemoveMember(request.UserId);
        return Result.Success();
    }
}
