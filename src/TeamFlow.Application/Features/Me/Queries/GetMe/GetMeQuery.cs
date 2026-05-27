using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Me.DTOs;

namespace TeamFlow.Application.Features.Me.Queries.GetMe;

public sealed record GetMeQuery : IQuery<MeDto>;

internal sealed class GetMeHandler : IQueryHandler<GetMeQuery, MeDto>
{
    private readonly ICurrentUser _currentUser;

    public GetMeHandler(ICurrentUser currentUser) => _currentUser = currentUser;

    public Task<Result<MeDto>> Handle(GetMeQuery request, CancellationToken ct)
    {
        var id = _currentUser.RequireUserId();
        var dto = new MeDto(
            id,
            _currentUser.Email,
            _currentUser.EmailVerified,
            _currentUser.FullName,
            _currentUser.AvatarUrl
        );
        return Task.FromResult(Result.Success(dto));
    }
}
