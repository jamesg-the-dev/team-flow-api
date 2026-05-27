using System.Security.Cryptography;

namespace TeamFlow.Application.Common.Security;

/// <summary>
/// Helpers to generate and hash workspace-invite tokens. The plain token is shown to the
/// inviter exactly once (in the POST response); only its SHA-256 hash is persisted.
/// </summary>
internal static class InviteToken
{
    public const int RandomBytes = 32;

    public static (string Plain, string Hash) Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(RandomBytes);
        var plain = Base64UrlEncode(bytes);
        return (plain, Hash(plain));
    }

    public static string Hash(string plain)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(plain);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
