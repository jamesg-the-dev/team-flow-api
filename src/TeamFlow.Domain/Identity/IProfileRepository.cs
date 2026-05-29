using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Identity;

public interface IProfileRepository : IRepository<Profile>
{
    /// <summary>Returns the profile for the given Supabase auth user id, or <c>null</c>.</summary>
    Task<Profile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    void Add(Profile profile);
}
