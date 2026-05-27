namespace TeamFlow.Api.Auth;

/// <summary>
/// Configuration bound from appsettings (Supabase section). Supabase issues JWTs whose
/// `iss` is your project's URL (e.g. https://abc.supabase.co/auth/v1) and signs them
/// with HS256 using the project's JWT secret.
/// </summary>
public sealed class SupabaseAuthOptions
{
    public const string SectionName = "Supabase";
    public string Url { get; init; } = string.Empty;
    public string JwtSecret { get; init; } = string.Empty;
    public string Audience { get; init; } = "authenticated";
}
