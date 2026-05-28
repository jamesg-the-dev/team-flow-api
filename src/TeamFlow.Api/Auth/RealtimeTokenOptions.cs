namespace TeamFlow.Api.Auth;

/// <summary>
/// Configuration for the realtime hub access token. This token is *separate* from the
/// Supabase access token: it is signed with a server-only key, scoped exclusively to the
/// realtime hub (<see cref="Audience"/>), and short-lived enough that exposure in a
/// WebSocket query string (the only place browsers can put it) is acceptable.
/// </summary>
public sealed class RealtimeTokenOptions
{
    public const string SectionName = "Realtime";

    /// <summary>HS256 signing key. Must be at least 32 bytes of high-entropy material.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "teamflow-api";

    public string Audience { get; set; } = "teamflow-realtime";

    /// <summary>Token lifetime in seconds. Keep small — clients refresh on reconnect.</summary>
    public int TtlSeconds { get; set; } = 60;

    /// <summary>JwtBearer authentication scheme name used by the hub.</summary>
    public const string Scheme = "Realtime";
}
