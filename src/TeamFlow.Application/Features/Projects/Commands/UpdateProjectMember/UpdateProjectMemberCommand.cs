using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Projects;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Projects.Commands.UpdateProjectMember;

public sealed record UpdateProjectMemberCommand(
    Guid ProjectId,
    Guid UserId,
    ProjectMemberRole Role
) : ICommand;

public sealed class UpdateProjectMemberValidator
    : AbstractValidator<UpdateProjectMemberCommand>
{
    public UpdateProjectMemberValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
    }
}

internal sealed class UpdateProjectMemberHandler
    : ICommandHandler<UpdateProjectMemberCommand>
{
    private readonly IProjectRepository _projects;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public UpdateProjectMemberHandler(
        IProjectRepository projects,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _projects = projects;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateProjectMemberCommand request, CancellationToken ct)
    {
        var project = await _projects.GetByIdWithMembersAsync(request.ProjectId, ct);
        if (project is null)
            return Error.NotFound("Project not found.");

        var actor = _currentUser.RequireUserId();
        if (
            !ProjectAuthorization.IsLead(project, actor)
            && !await _workspaces.IsOwnerOrAdminAsync(project.WorkspaceId, actor, ct)
        )
            return Error.Forbidden(
                "Only project leads or workspace owners/admins can change member roles."
            );

        project.UpdateMemberRole(request.UserId, request.Role);
        return Result.Success();
    }
}
