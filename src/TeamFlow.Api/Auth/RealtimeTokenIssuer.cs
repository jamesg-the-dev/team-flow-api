using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace TeamFlow.Api.Auth;

public interface IRealtimeTokenIssuer
{
    RealtimeTokenIssueResult Issue(Guid userId);
}

public readonly record struct RealtimeTokenIssueResult(string Token, DateTimeOffset ExpiresAt);

internal sealed class RealtimeTokenIssuer : IRealtimeTokenIssuer
{
    private readonly RealtimeTokenOptions _opts;
    private readonly SigningCredentials _credentials;
    private readonly JwtSecurityTokenHandler _handler = new() { SetDefaultTimesOnTokenCreation = false };

    public RealtimeTokenIssuer(IOptions<RealtimeTokenOptions> opts)
    {
        _opts = opts.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey))
        {
            KeyId = "realtime",
        };
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public RealtimeTokenIssueResult Issue(Guid userId)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddSeconds(_opts.TtlSeconds);

        var jwt = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),
            ],
            notBefore: now,
            expires: expires,
            signingCredentials: _credentials
        );

        return new RealtimeTokenIssueResult(_handler.WriteToken(jwt), new DateTimeOffset(expires));
    }
}
