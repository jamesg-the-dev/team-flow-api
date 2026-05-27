using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Workspaces;

public interface IWorkspaceRepository : IRepository<Workspace>
{
    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Workspace?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    void Add(Workspace workspace);
    void Remove(Workspace workspace);
}
