using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Auth.DTOs;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Auth.Commands.Bootstrap;

/// <summary>
/// First-login provisioning. Idempotent — if the caller already belongs to one or more
/// workspaces the first one is returned; otherwise a personal workspace is created.
/// The caller is identified by the Supabase JWT (<see cref="ICurrentUser"/>).
/// </summary>
public sealed record BootstrapCommand : ICommand<BootstrapDto>;

internal sealed class BootstrapHandler : ICommandHandler<BootstrapCommand, BootstrapDto>
{
    private const int MaxSlugAttempts = 5;

    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public BootstrapHandler(IWorkspaceRepository workspaces, ICurrentUser currentUser)
    {
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result<BootstrapDto>> Handle(BootstrapCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();

        var existingIds = await _workspaces.ListIdsForUserAsync(userId, ct);
        if (existingIds.Count > 0)
        {
            var existing = await _workspaces.GetByIdAsync(existingIds[0], ct);
            if (existing is not null)
                return new BootstrapDto(existing.Id, existing.Slug, existing.Name, false);
        }

        var (slug, name) = await GenerateUniqueWorkspaceAsync(ct);
        var workspace = Workspace.Create(slug, name, userId);
        _workspaces.Add(workspace);

        return new BootstrapDto(workspace.Id, workspace.Slug, workspace.Name, true);
    }

    private async Task<(string Slug, string Name)> GenerateUniqueWorkspaceAsync(CancellationToken ct)
    {
        var baseSlug = DeriveBaseSlug(_currentUser.Email, _currentUser.FullName);
        var displayName = string.IsNullOrWhiteSpace(_currentUser.FullName)
            ? "Personal Workspace"
            : $"{_currentUser.FullName!.Trim()}'s Workspace";

        for (var attempt = 0; attempt < MaxSlugAttempts; attempt++)
        {
            var candidate = attempt == 0 ? baseSlug : $"{baseSlug}-{ShortToken()}";
            if (!await _workspaces.SlugExistsAsync(candidate, ct))
                return (candidate, displayName);
        }

        // Fallback: guaranteed unique
        return ($"workspace-{Guid.NewGuid():N}".Substring(0, 24), displayName);
    }

    private static string DeriveBaseSlug(string? email, string? fullName)
    {
        var source = !string.IsNullOrWhiteSpace(email)
            ? email!.Split('@')[0]
            : !string.IsNullOrWhiteSpace(fullName)
                ? fullName
                : "workspace";

        var slug = new string(
            source
                .ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                .ToArray()
        )
            .Trim('-');

        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");

        if (slug.Length == 0)
            slug = "workspace";
        if (slug.Length > 40)
            slug = slug[..40].TrimEnd('-');

        return slug;
    }

    private static string ShortToken() =>
        Guid.NewGuid().ToString("N")[..6];
}
