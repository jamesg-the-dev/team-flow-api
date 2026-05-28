using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Projects;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Projects.Commands.AddProjectMember;

public sealed record AddProjectMemberCommand(Guid ProjectId, Guid UserId, ProjectMemberRole Role)
    : ICommand;

public sealed class AddProjectMemberValidator : AbstractValidator<AddProjectMemberCommand>
{
    public AddProjectMemberValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
    }
}

internal sealed class AddProjectMemberHandler : ICommandHandler<AddProjectMemberCommand>
{
    private readonly IProjectRepository _projects;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public AddProjectMemberHandler(
        IProjectRepository projects,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _projects = projects;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(AddProjectMemberCommand request, CancellationToken ct)
    {
        var project = await _projects.GetByIdWithMembersAsync(request.ProjectId, ct);
        if (project is null)
            return Error.NotFound($"Project '{request.ProjectId}' not found.");

        var actor = _currentUser.RequireUserId();
        if (
            !ProjectAuthorization.IsLead(project, actor)
            && !await _workspaces.IsOwnerOrAdminAsync(project.WorkspaceId, actor, ct)
        )
            return Error.Forbidden(
                "Only project leads or workspace owners/admins can add project members."
            );

        if (!await _workspaces.IsMemberAsync(project.WorkspaceId, request.UserId, ct))
            return Error.Validation("User must be a workspace member before joining a project.");

        try
        {
            project.AddMember(request.UserId, request.Role);
            return Result.Success();
        }
        catch (TeamFlow.Domain.SeedWork.DomainException ex)
            when (
                ex.Message.Contains("already a project member", StringComparison.OrdinalIgnoreCase)
            )
        {
            return Error.Conflict(ex.Message);
        }
    }
}
