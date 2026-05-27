using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Features.Tasks.Commands.AddTaskComment;
using TeamFlow.Application.Features.Tasks.Commands.AssignTask;
using TeamFlow.Application.Features.Tasks.Commands.CreateTask;
using TeamFlow.Application.Features.Tasks.Commands.MoveTask;
using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Application.Features.Tasks.Queries.GetProjectBoard;
using TeamFlow.Application.Features.Tasks.Queries.GetTaskById;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Controllers;

[ApiController]
[Authorize(Policy = "authenticated")]
[Route("api/v1")]
[Produces("application/json")]
public sealed class TasksController : ControllerBase
{
    private readonly ISender _sender;
    public TasksController(ISender sender) => _sender = sender;

    [HttpGet("projects/{projectId:guid}/board")]
    [ProducesResponseType(typeof(IReadOnlyDictionary<TaskColumn, IReadOnlyList<TaskBoardCardDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetBoard(Guid projectId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetProjectBoardQuery(projectId), ct);
        return result.ToActionResult().Result!;
    }

    [HttpPost("projects/{projectId:guid}/tasks")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TaskDto>> Create(Guid projectId, [FromBody] CreateTaskRequest body, CancellationToken ct)
    {
        var cmd = new CreateTaskCommand(projectId, body.Title, body.Description,
            body.Priority, body.AssigneeId, body.EstimateHours, body.DueDate);
        var result = await _sender.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : result.ToActionResult().Result!;
    }

    [HttpGet("tasks/{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> Get(Guid id, CancellationToken ct)
        => (await _sender.Send(new GetTaskByIdQuery(id), ct)).ToActionResult();

    [HttpPost("tasks/{id:guid}/move")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Move(Guid id, [FromBody] MoveTaskRequest body, CancellationToken ct)
        => (await _sender.Send(new MoveTaskCommand(id, body.TargetColumn, body.BeforeTaskId, body.AfterTaskId), ct)).ToActionResult();

    [HttpPost("tasks/{id:guid}/assignee")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Assign(Guid id, [FromBody] AssignRequest body, CancellationToken ct)
        => (await _sender.Send(new AssignTaskCommand(id, body.AssigneeId), ct)).ToActionResult();

    [HttpPost("tasks/{id:guid}/comments")]
    [ProducesResponseType(typeof(TaskCommentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskCommentDto>> Comment(Guid id, [FromBody] AddCommentRequest body, CancellationToken ct)
        => (await _sender.Send(new AddTaskCommentCommand(id, body.Body, body.ParentId), ct)).ToActionResult();

    public sealed record CreateTaskRequest(string Title, string? Description, PriorityLevel Priority,
        Guid? AssigneeId, decimal? EstimateHours, DateOnly? DueDate);
    public sealed record MoveTaskRequest(TaskColumn TargetColumn, Guid? BeforeTaskId, Guid? AfterTaskId);
    public sealed record AssignRequest(Guid? AssigneeId);
    public sealed record AddCommentRequest(string Body, Guid? ParentId);
}
