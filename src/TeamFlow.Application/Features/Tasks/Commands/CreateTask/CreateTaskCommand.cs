using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Projects;
using TeamFlow.Domain.Tasks;

namespace TeamFlow.Application.Features.Tasks.Commands.CreateTask;

public sealed record CreateTaskCommand(
    Guid ProjectId,
    string Title,
    string? Description,
    PriorityLevel Priority,
    Guid? AssigneeId,
    decimal? EstimateHours,
    DateOnly? DueDate,
    TaskColumn Column
) : ICommand<TaskDto>;

public sealed class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(8000);
        RuleFor(x => x.EstimateHours).GreaterThanOrEqualTo(0).When(x => x.EstimateHours.HasValue);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Column).IsInEnum();
    }
}

internal sealed class CreateTaskHandler : ICommandHandler<CreateTaskCommand, TaskDto>
{
    private readonly IProjectRepository _projects;
    private readonly ITaskRepository _tasks;
    private readonly ICurrentUser _currentUser;

    public CreateTaskHandler(
        IProjectRepository projects,
        ITaskRepository tasks,
        ICurrentUser currentUser
    )
    {
        _projects = projects;
        _tasks = tasks;
        _currentUser = currentUser;
    }

    public async Task<Result<TaskDto>> Handle(CreateTaskCommand request, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(request.ProjectId, ct);
        if (project is null)
            return Error.NotFound($"Project '{request.ProjectId}' not found.");


        // Append to bottom of the specified column.
        var (_, max) = await _tasks.GetColumnPositionBoundsAsync(
            project.Id,
            request.Column,
            ct
        );
        var newPosition = (max ?? 0m) + 1024m;

        var task = TaskItem.Create(
            project.WorkspaceId,
            project.Id,
            number: project.AllocateTaskNumber(),
            title: request.Title,
            reporterId: _currentUser.RequireUserId(),
            position: newPosition,
            description: request.Description,
            priority: request.Priority,
            assigneeId: request.AssigneeId,
            estimateHours: request.EstimateHours,
            dueDate: request.DueDate,
            column: request.Column
        );

        _tasks.Add(task);

        return new TaskDto(
            task.Id,
            task.ProjectId,
            task.Number,
            task.Title,
            task.Description,
            task.Column,
            task.Priority,
            task.Position,
            task.AssigneeId,
            task.ReporterId,
            task.EstimateHours,
            task.DueDate,
            task.CompletedAt,
            task.CreatedAt,
            task.UpdatedAt
        );
    }
}
