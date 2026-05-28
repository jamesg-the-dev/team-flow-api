using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Projects;

public interface IProjectRepository : IRepository<Project>
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Project?> GetByIdWithMembersAsync(Guid id, CancellationToken ct = default);
    Task<bool> KeyExistsAsync(Guid workspaceId, string key, CancellationToken ct = default);

    /// <summary>Cheap project-membership check used by handlers for authorization.</summary>
    Task<bool> IsMemberAsync(Guid projectId, Guid userId, CancellationToken ct = default);

    void Add(Project project);
    void Remove(Project project);
}
