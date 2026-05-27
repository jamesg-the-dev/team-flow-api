using TeamFlow.Domain.Enums;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Tasks;

public interface ITaskRepository : IRepository<TaskItem>
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaskItem?> GetByIdWithChildrenAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns ordered tasks for a project + column. Caller projects to DTOs.</summary>
    Task<IReadOnlyList<TaskItem>> ListByColumnAsync(
        Guid projectId,
        TaskColumn column,
        CancellationToken ct = default
    );

    /// <summary>Returns the min / max Position in a column (used to compute fractional insert positions).</summary>
    Task<(decimal? Min, decimal? Max)> GetColumnPositionBoundsAsync(
        Guid projectId,
        TaskColumn column,
        CancellationToken ct = default
    );

    /// <summary>Returns the position immediately before/after a target task in the same column.</summary>
    Task<decimal?> GetNeighbourPositionAsync(
        Guid taskId,
        bool before,
        CancellationToken ct = default
    );

    void Add(TaskItem task);
    void Remove(TaskItem task);
}
