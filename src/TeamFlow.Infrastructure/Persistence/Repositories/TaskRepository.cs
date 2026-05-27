using Microsoft.EntityFrameworkCore;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.SeedWork;
using TeamFlow.Domain.Tasks;

namespace TeamFlow.Infrastructure.Persistence.Repositories;

internal sealed class TaskRepository : ITaskRepository
{
    private readonly TeamFlowDbContext _ctx;
    public TaskRepository(TeamFlowDbContext ctx, IUnitOfWork unitOfWork) { _ctx = ctx; UnitOfWork = unitOfWork; }
    public IUnitOfWork UnitOfWork { get; }

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _ctx.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<TaskItem?> GetByIdWithChildrenAsync(Guid id, CancellationToken ct = default) =>
        _ctx.Tasks
            .Include(t => t.Tags)
            .Include(t => t.Watchers)
            .Include(t => t.Dependencies)
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<TaskItem>> ListByColumnAsync(Guid projectId, TaskColumn column, CancellationToken ct = default) =>
        await _ctx.Tasks
            .Where(t => t.ProjectId == projectId && t.Column == column)
            .OrderBy(t => t.Position)
            .ToListAsync(ct);

    public async Task<(decimal? Min, decimal? Max)> GetColumnPositionBoundsAsync(Guid projectId, TaskColumn column, CancellationToken ct = default)
    {
        var bounds = await _ctx.Tasks
            .Where(t => t.ProjectId == projectId && t.Column == column)
            .GroupBy(_ => 1)
            .Select(g => new { Min = (decimal?)g.Min(t => t.Position), Max = (decimal?)g.Max(t => t.Position) })
            .FirstOrDefaultAsync(ct);
        return (bounds?.Min, bounds?.Max);
    }

    public async Task<decimal?> GetNeighbourPositionAsync(Guid taskId, bool before, CancellationToken ct = default)
    {
        var pivot = await _ctx.Tasks.AsNoTracking()
            .Where(t => t.Id == taskId)
            .Select(t => new { t.ProjectId, t.Column, t.Position })
            .FirstOrDefaultAsync(ct);
        if (pivot is null) return null;

        var sibling = before
            ? await _ctx.Tasks.AsNoTracking()
                .Where(t => t.ProjectId == pivot.ProjectId && t.Column == pivot.Column && t.Position < pivot.Position)
                .OrderByDescending(t => t.Position)
                .Select(t => (decimal?)t.Position)
                .FirstOrDefaultAsync(ct)
            : await _ctx.Tasks.AsNoTracking()
                .Where(t => t.ProjectId == pivot.ProjectId && t.Column == pivot.Column && t.Position > pivot.Position)
                .OrderBy(t => t.Position)
                .Select(t => (decimal?)t.Position)
                .FirstOrDefaultAsync(ct);

        return sibling;
    }

    public void Add(TaskItem task) => _ctx.Tasks.Add(task);
    public void Remove(TaskItem task) => _ctx.Tasks.Remove(task);
}
