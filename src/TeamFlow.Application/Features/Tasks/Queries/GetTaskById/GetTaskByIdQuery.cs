using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Domain.Tasks;

namespace TeamFlow.Application.Features.Tasks.Queries.GetTaskById;

public sealed record GetTaskByIdQuery(Guid Id) : IQuery<TaskDto>;

internal sealed class GetTaskByIdHandler : IQueryHandler<GetTaskByIdQuery, TaskDto>
{
    private readonly ITaskRepository _tasks;

    public GetTaskByIdHandler(ITaskRepository tasks) => _tasks = tasks;

    public async Task<Result<TaskDto>> Handle(GetTaskByIdQuery request, CancellationToken ct)
    {
        var t = await _tasks.GetByIdAsync(request.Id, ct);
        if (t is null)
            return Error.NotFound($"Task '{request.Id}' not found.");

        return new TaskDto(
            t.Id,
            t.ProjectId,
            t.Number,
            t.Title,
            t.Description,
            t.Column,
            t.Priority,
            t.Position,
            t.AssigneeId,
            t.ReporterId,
            t.EstimateHours,
            t.DueDate,
            t.CompletedAt,
            t.CreatedAt,
            t.UpdatedAt
        );
    }
}
