using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Application.Features.Tasks.Queries.GetTaskByNumber;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

internal sealed class GetTaskByNumberQueryService : IGetTaskByNumberQueryService
{
    private readonly TeamFlowDbContext _ctx;

    public GetTaskByNumberQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    public Task<TaskDto?> ExecuteAsync(Guid projectId, int number, CancellationToken ct) =>
        _ctx
            .Tasks.AsNoTracking()
            .Where(t => t.ProjectId == projectId && t.Number == number)
            .Select(t => new TaskDto(
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
            ))
            .FirstOrDefaultAsync(ct);
}
