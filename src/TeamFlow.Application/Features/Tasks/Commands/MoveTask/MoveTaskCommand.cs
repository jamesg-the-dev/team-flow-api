using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Tasks;

namespace TeamFlow.Application.Features.Tasks.Commands.MoveTask;

/// <summary>
/// Reposition a task on the kanban board.
/// Provide either BeforeTaskId, AfterTaskId, or neither (append to end of column).
/// </summary>
public sealed record MoveTaskCommand(
    Guid TaskId,
    TaskColumn TargetColumn,
    Guid? BeforeTaskId,
    Guid? AfterTaskId
) : ICommand;

public sealed class MoveTaskValidator : AbstractValidator<MoveTaskCommand>
{
    public MoveTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.TargetColumn).IsInEnum();
        RuleFor(x => x)
            .Must(c => !(c.BeforeTaskId.HasValue && c.AfterTaskId.HasValue))
            .WithMessage("Specify either BeforeTaskId or AfterTaskId, not both.");
    }
}

internal sealed class MoveTaskHandler : ICommandHandler<MoveTaskCommand>
{
    private readonly ITaskRepository _tasks;
    private readonly IDateTimeProvider _clock;

    public MoveTaskHandler(ITaskRepository tasks, IDateTimeProvider clock)
    {
        _tasks = tasks;
        _clock = clock;
    }

    public async Task<Result> Handle(MoveTaskCommand request, CancellationToken ct)
    {
        var task = await _tasks.GetByIdAsync(request.TaskId, ct);
        if (task is null)
            return Error.NotFound($"Task '{request.TaskId}' not found.");

        var newPosition = await ComputePositionAsync(task.ProjectId, request, ct);
        task.MoveTo(request.TargetColumn, newPosition, _clock.UtcNow);
        return Result.Success();
    }

    private async Task<decimal> ComputePositionAsync(
        Guid projectId,
        MoveTaskCommand req,
        CancellationToken ct
    )
    {
        // Append to bottom of target column
        if (req.BeforeTaskId is null && req.AfterTaskId is null)
        {
            var (_, max) = await _tasks.GetColumnPositionBoundsAsync(
                projectId,
                req.TargetColumn,
                ct
            );
            return (max ?? 0m) + 1024m;
        }

        if (req.BeforeTaskId is { } beforeId)
        {
            var prev = await _tasks.GetNeighbourPositionAsync(beforeId, before: true, ct);
            var beforePos =
                (await _tasks.GetByIdAsync(beforeId, ct))?.Position
                ?? throw new InvalidOperationException("Target before-task not found.");
            return prev is null ? beforePos - 1024m : (prev.Value + beforePos) / 2m;
        }
        else
        {
            var afterId = req.AfterTaskId!.Value;
            var next = await _tasks.GetNeighbourPositionAsync(afterId, before: false, ct);
            var afterPos =
                (await _tasks.GetByIdAsync(afterId, ct))?.Position
                ?? throw new InvalidOperationException("Target after-task not found.");
            return next is null ? afterPos + 1024m : (next.Value + afterPos) / 2m;
        }
    }
}
