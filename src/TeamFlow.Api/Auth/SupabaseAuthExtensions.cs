using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TeamFlow.Application.Common.Abstractions;

namespace TeamFlow.Api.Auth;

public static class SupabaseAuthExtensions
{
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

        var supabaseOpts =
            configuration.GetSection(SupabaseAuthOptions.SectionName).Get<SupabaseAuthOptions>()
            ?? throw new InvalidOperationException("Missing Supabase configuration.");
        if (string.IsNullOrWhiteSpace(supabaseOpts.JwtSecret))
            throw new InvalidOperationException("Supabase:JwtSecret is required.");

        var realtimeOpts =
            configuration.GetSection(RealtimeTokenOptions.SectionName).Get<RealtimeTokenOptions>()
            ?? new RealtimeTokenOptions();
        EnsureRealtimeSigningKey(realtimeOpts, services);
        services.Configure<RealtimeTokenOptions>(o =>
        {
            o.SigningKey = realtimeOpts.SigningKey;
            o.Issuer = realtimeOpts.Issuer;
            o.Audience = realtimeOpts.Audience;
            o.TtlSeconds = realtimeOpts.TtlSeconds;
        });
        services.AddSingleton<IRealtimeTokenIssuer, RealtimeTokenIssuer>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(
                JwtBearerDefaults.AuthenticationScheme,
                options => ConfigureSupabaseScheme(options, supabaseOpts)
            )
            .AddJwtBearer(
                RealtimeTokenOptions.Scheme,
                options => ConfigureRealtimeScheme(options, realtimeOpts)
            );

        services
            .AddAuthorizationBuilder()
            .AddPolicy(
                "authenticated",
                p =>
                    p.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
            )
            .AddPolicy(
                "workspace-member",
                p =>
                    p.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
            )
            .AddPolicy(
                "realtime-hub",
                p =>
                    p.AddAuthenticationSchemes(RealtimeTokenOptions.Scheme)
                        .RequireAuthenticatedUser()
            );

        return services;
    }

    private static void ConfigureSupabaseScheme(JwtBearerOptions options, SupabaseAuthOptions opts)
    {
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.JwtSecret)),
            NameClaimType = "sub",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    }

    private static void ConfigureRealtimeScheme(JwtBearerOptions options, RealtimeTokenOptions opts)
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = opts.Issuer,
            ValidateAudience = true,
            ValidAudience = opts.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.SigningKey)),
            NameClaimType = "sub",
            ClockSkew = TimeSpan.FromSeconds(5),
        };

        // Browsers cannot set the Authorization header on the WebSocket upgrade, so the
        // SignalR JS client passes the hub token as `?access_token=...`. We honor that
        // ONLY on the hub path and ONLY for this scheme.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path;
                if (!path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
                    return Task.CompletedTask;

                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                    context.Token = accessToken!;
                return Task.CompletedTask;
            },
        };
    }

    private static void EnsureRealtimeSigningKey(
        RealtimeTokenOptions opts,
        IServiceCollection services
    )
    {
        if (
            !string.IsNullOrWhiteSpace(opts.SigningKey)
            && Encoding.UTF8.GetByteCount(opts.SigningKey) >= 32
        )
        {
            return;
        }

        opts.SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        services.AddSingleton<IStartupFilter>(new RealtimeKeyWarningStartupFilter());
    }

    private sealed class RealtimeKeyWarningStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                var logger = app
                    .ApplicationServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("TeamFlow.Auth.Realtime");
                logger.LogWarning(
                    "Realtime:SigningKey is not configured (or shorter than 32 bytes). "
                        + "An ephemeral key has been generated for this process — hub tokens "
                        + "will not validate across instances or restarts. Configure "
                        + "'Realtime:SigningKey' (>= 32 bytes of high-entropy material) for production."
                );
                next(app);
            };
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

    public bool EmailVerified => bool.TryParse(Claim("email_verified"), out var v) && v;

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
            return
                doc.RootElement.TryGetProperty(key, out var v)
                && v.ValueKind == System.Text.Json.JsonValueKind.String
                ? v.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }
}
