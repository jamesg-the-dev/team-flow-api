using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TeamFlow.Application.Common.Abstractions;

namespace TeamFlow.Api.Auth;

public static class SupabaseAuthExtensions
{
    /// <summary>
    /// Configures JWT bearer authentication compatible with Supabase Auth.
    /// Supabase signs tokens with HS256 using the project's JWT secret. The `sub` claim
    /// carries the Supabase user id (uuid).
    /// </summary>
    public static IServiceCollection AddSupabaseAuth(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<SupabaseAuthOptions>(
            configuration.GetSection(SupabaseAuthOptions.SectionName)
        );

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var opts =
                    configuration
                        .GetSection(SupabaseAuthOptions.SectionName)
                        .Get<SupabaseAuthOptions>()
                    ?? throw new InvalidOperationException("Missing Supabase configuration.");
                if (string.IsNullOrWhiteSpace(opts.JwtSecret))
                    throw new InvalidOperationException("Supabase:JwtSecret is required.");

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrWhiteSpace(opts.Url),
                    ValidIssuer = string.IsNullOrWhiteSpace(opts.Url)
                        ? null
                        : $"{opts.Url.TrimEnd('/')}/auth/v1",
                    ValidateAudience = true,
                    ValidAudience = opts.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(opts.JwtSecret)
                    ),
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("authenticated", p => p.RequireAuthenticatedUser());
            options.AddPolicy("workspace-member", p => p.RequireAuthenticatedUser());
            // App-level row authorization is enforced inside handlers; Supabase RLS is the
            // database-level safety net.
        });

        return services;
    }
}

internal sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public bool IsAuthenticated => _accessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var sub =
                _accessor.HttpContext?.User?.FindFirst("sub")?.Value
                ?? _accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public Guid RequireUserId() =>
        UserId
        ?? throw new UnauthorizedAccessException("No authenticated Supabase user on the request.");

    public string? Email => Claim("email");

    public bool EmailVerified =>
        bool.TryParse(Claim("email_verified"), out var v) && v;

    public string? FullName => MetadataString("full_name") ?? MetadataString("name");

    public string? AvatarUrl => MetadataString("avatar_url") ?? MetadataString("picture");

    private string? Claim(string type) => _accessor.HttpContext?.User?.FindFirst(type)?.Value;

    private string? MetadataString(string key)
    {
        var raw = Claim("user_metadata");
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            return doc.RootElement.TryGetProperty(key, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String
                ? v.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }
}
