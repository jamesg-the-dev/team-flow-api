using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Me.DTOs;
using TeamFlow.Domain.Identity;

namespace TeamFlow.Application.Features.Me.Queries.GetMyProfile;

/// <summary>
/// Returns the calling Supabase user's TeamFlow profile, or <c>null</c> if one has not been
/// created yet. The first call from a freshly-signed-up user will typically return <c>null</c>;
/// the client should then POST/PUT <c>/me/profile</c> to provision it.
/// </summary>
public sealed record GetMyProfileQuery : IQuery<ProfileDto?>;

internal sealed class GetMyProfileHandler : IQueryHandler<GetMyProfileQuery, ProfileDto?>
{
    private readonly IProfileRepository _profiles;
    private readonly ICurrentUser _currentUser;

    public GetMyProfileHandler(IProfileRepository profiles, ICurrentUser currentUser)
    {
        _profiles = profiles;
        _currentUser = currentUser;
    }

    public async Task<Result<ProfileDto?>> Handle(GetMyProfileQuery request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        var profile = await _profiles.GetByUserIdAsync(userId, ct);
        return Result.Success<ProfileDto?>(profile is null ? null : ProfileDto.From(profile));
    }
}
