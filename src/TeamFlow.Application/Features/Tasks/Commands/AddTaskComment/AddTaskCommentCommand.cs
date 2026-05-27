using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Domain.Tasks;

namespace TeamFlow.Application.Features.Tasks.Commands.AddTaskComment;

public sealed record AddTaskCommentCommand(Guid TaskId, string Body, Guid? ParentId) : ICommand<TaskCommentDto>;

public sealed class AddTaskCommentValidator : AbstractValidator<AddTaskCommentCommand>
{
    public AddTaskCommentValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(10_000);
    }
}

internal sealed class AddTaskCommentHandler : ICommandHandler<AddTaskCommentCommand, TaskCommentDto>
{
    private readonly ITaskRepository _tasks;
    private readonly ICurrentUser _currentUser;

    public AddTaskCommentHandler(ITaskRepository tasks, ICurrentUser currentUser)
    {
        _tasks = tasks; _currentUser = currentUser;
    }

    public async Task<Result<TaskCommentDto>> Handle(AddTaskCommentCommand request, CancellationToken ct)
    {
        var task = await _tasks.GetByIdWithChildrenAsync(request.TaskId, ct);
        if (task is null) return Error.NotFound($"Task '{request.TaskId}' not found.");

        var comment = task.AddComment(_currentUser.RequireUserId(), request.Body, request.ParentId);
        return new TaskCommentDto(comment.Id, comment.TaskId, comment.AuthorId, comment.ParentId,
            comment.Body, comment.CreatedAt, comment.EditedAt);
    }
}
