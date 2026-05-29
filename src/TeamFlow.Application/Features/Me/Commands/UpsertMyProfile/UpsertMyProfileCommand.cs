using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Me.DTOs;
using TeamFlow.Domain.Identity;

namespace TeamFlow.Application.Features.Me.Commands.UpsertMyProfile;

/// <summary>
/// Creates or updates the caller's profile. The <c>UserId</c> is taken from the JWT —
/// never from the request body — so this command is safe to expose as <c>PUT /me/profile</c>.
/// </summary>
public sealed record UpsertMyProfileCommand(
    string FullName,
    string? DisplayName,
    string? AvatarPath,
    string? Bio,
    string? Timezone,
    string? Locale
) : ICommand<ProfileDto>;

public sealed class UpsertMyProfileValidator : AbstractValidator<UpsertMyProfileCommand>
{
    public UpsertMyProfileValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DisplayName).MaximumLength(100);
        RuleFor(x => x.AvatarPath).MaximumLength(2000);
        RuleFor(x => x.Bio).MaximumLength(500);
        RuleFor(x => x.Timezone).MaximumLength(64);
        RuleFor(x => x.Locale).MaximumLength(16);
    }
}

internal sealed class UpsertMyProfileHandler : ICommandHandler<UpsertMyProfileCommand, ProfileDto>
{
    private readonly IProfileRepository _profiles;
    private readonly ICurrentUser _currentUser;

    public UpsertMyProfileHandler(IProfileRepository profiles, ICurrentUser currentUser)
    {
        _profiles = profiles;
        _currentUser = currentUser;
    }

    public async Task<Result<ProfileDto>> Handle(
        UpsertMyProfileCommand request,
        CancellationToken ct
    )
    {
        var userId = _currentUser.RequireUserId();
        var existing = await _profiles.GetByUserIdAsync(userId, ct);

        if (existing is null)
        {
            var created = Profile.Create(
                userId,
                request.FullName,
                request.DisplayName,
                request.AvatarPath,
                request.Bio,
                request.Timezone,
                request.Locale
            );
            _profiles.Add(created);
            return Result.Success(ProfileDto.From(created));
        }

        existing.Rename(request.FullName);
        existing.SetDisplayName(request.DisplayName);
        existing.SetAvatarPath(request.AvatarPath);
        existing.SetBio(request.Bio);
        if (!string.IsNullOrWhiteSpace(request.Timezone))
            existing.SetTimezone(request.Timezone);
        if (!string.IsNullOrWhiteSpace(request.Locale))
            existing.SetLocale(request.Locale);

        return Result.Success(ProfileDto.From(existing));
    }
}
