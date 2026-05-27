using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Workspaces;

public interface IWorkspaceRepository : IRepository<Workspace>
{
    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads the aggregate including both <see cref="Workspace.Members"/> and <see cref="Workspace.Invites"/>.</summary>
    Task<Workspace?> GetByIdWithInvitesAsync(Guid id, CancellationToken ct = default);

    Task<Workspace?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);

    /// <summary>Returns ids of workspaces the user belongs to (membership join).</summary>
    Task<IReadOnlyList<Guid>> ListIdsForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Cheap membership check used by handlers for authorization.</summary>
    Task<bool> IsMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Loads the workspace whose <see cref="WorkspaceInvite.TokenHash"/> matches, including its
    /// invites and members so the aggregate can validate and accept the invite atomically.
    /// </summary>
    Task<Workspace?> GetByInviteTokenHashAsync(
        string tokenHash,
        CancellationToken ct = default
    );

    void Add(Workspace workspace);
    void Remove(Workspace workspace);
}
