using FluentValidation;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Tasks;

namespace TeamFlow.Application.Features.Tasks.Commands.AssignTask;

public sealed record AssignTaskCommand(Guid TaskId, Guid? AssigneeId) : ICommand;

public sealed class AssignTaskValidator : AbstractValidator<AssignTaskCommand>
{
    public AssignTaskValidator() => RuleFor(x => x.TaskId).NotEmpty();
}

internal sealed class AssignTaskHandler : ICommandHandler<AssignTaskCommand>
{
    private readonly ITaskRepository _tasks;
    public AssignTaskHandler(ITaskRepository tasks) => _tasks = tasks;

    public async Task<Result> Handle(AssignTaskCommand request, CancellationToken ct)
    {
        var task = await _tasks.GetByIdAsync(request.TaskId, ct);
        if (task is null) return Error.NotFound($"Task '{request.TaskId}' not found.");
        task.Assign(request.AssigneeId);
        return Result.Success();
    }
}
