using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Identity;

/// <summary>
/// Application-level profile for a Supabase-authenticated user.
/// Mirrors a row in <c>auth.users</c> (managed by Supabase) with TeamFlow-specific data
/// (display name, avatar, preferences). The <see cref="UserId"/> column holds the Supabase
/// user id (the <c>sub</c> claim on the JWT) and is unique.
/// </summary>
public sealed class Profile : AuditableAggregateRoot
{
    /// <summary>Supabase auth user id (matches <c>auth.users.id</c> / JWT <c>sub</c> claim).</summary>
    public Guid UserId { get; private set; }

    public string FullName { get; private set; } = null!;

    /// <summary>Optional short handle / nickname shown in mentions and chips.</summary>
    public string? DisplayName { get; private set; }

    /// <summary>Storage path or absolute URL of the avatar image (e.g. Supabase Storage key).</summary>
    public string? AvatarPath { get; private set; }

    /// <summary>Short free-form bio shown on the profile page (nullable, max 500 chars).</summary>
    public string? Bio { get; private set; }

    public string Timezone { get; private set; } = "UTC";

    public string Locale { get; private set; } = "en-US";

    private Profile() { }

    private Profile(Guid id, Guid userId, string fullName)
        : base(id)
    {
        UserId = userId;
        FullName = fullName;
    }

    public static Profile Create(
        Guid userId,
        string fullName,
        string? displayName = null,
        string? avatarPath = null,
        string? bio = null,
        string? timezone = null,
        string? locale = null
    )
    {
        if (userId == Guid.Empty)
            throw DomainException.Invariant("UserId is required.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw DomainException.Invariant("Full name is required.");

        var profile = new Profile(Guid.CreateVersion7(), userId, fullName.Trim())
        {
            DisplayName = Normalize(displayName),
            AvatarPath = Normalize(avatarPath),
            Bio = Normalize(bio, maxLen: 500),
            Timezone = string.IsNullOrWhiteSpace(timezone) ? "UTC" : timezone.Trim(),
            Locale = string.IsNullOrWhiteSpace(locale) ? "en-US" : locale.Trim(),
        };
        return profile;
    }

    public void Rename(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw DomainException.Invariant("Full name is required.");
        FullName = fullName.Trim();
    }

    public void SetDisplayName(string? displayName) => DisplayName = Normalize(displayName);

    public void SetAvatarPath(string? avatarPath) => AvatarPath = Normalize(avatarPath);

    public void SetBio(string? bio) => Bio = Normalize(bio, maxLen: 500);

    public void SetTimezone(string timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
            throw DomainException.Invariant("Timezone is required.");
        Timezone = timezone.Trim();
    }

    public void SetLocale(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            throw DomainException.Invariant("Locale is required.");
        Locale = locale.Trim();
    }

    private static string? Normalize(string? value, int? maxLen = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        if (maxLen is int max && trimmed.Length > max)
            throw DomainException.Invariant($"Value exceeds maximum length of {max} characters.");
        return trimmed;
    }
}
