using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Projects;
using TeamFlow.Domain.Tasks;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Tasks.Commands.DeleteTask;

/// <summary>
/// Soft-deletes a task. Allowed when the caller is the task's reporter, the current assignee,
/// the project's Lead, or a workspace Owner/Admin.
/// </summary>
public sealed record DeleteTaskCommand(Guid TaskId) : ICommand;

internal sealed class DeleteTaskHandler : ICommandHandler<DeleteTaskCommand>
{
    private readonly ITaskRepository _tasks;
    private readonly IProjectRepository _projects;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public DeleteTaskHandler(
        ITaskRepository tasks,
        IProjectRepository projects,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser,
        IDateTimeProvider clock
    )
    {
        _tasks = tasks;
        _projects = projects;
        _workspaces = workspaces;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken ct)
    {
        var task = await _tasks.GetByIdAsync(request.TaskId, ct);
        if (task is null)
            return Error.NotFound("Task not found.");

        var actor = _currentUser.RequireUserId();
        var isOriginator = task.ReporterId == actor || task.AssigneeId == actor;
        if (!isOriginator)
        {
            var project = await _projects.GetByIdWithMembersAsync(task.ProjectId, ct);
            if (project is null)
                return Error.NotFound("Project not found.");
            if (
                !ProjectAuthorization.IsLead(project, actor)
                && !await _workspaces.IsOwnerOrAdminAsync(task.WorkspaceId, actor, ct)
            )
                return Error.Forbidden(
                    "You can only delete tasks you reported or were assigned to; otherwise project leads or workspace owners/admins must do it."
                );
        }

        task.SoftDelete(_clock.UtcNow);
        return Result.Success();
    }
}
