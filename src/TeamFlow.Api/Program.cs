using HealthChecks.NpgSql;
using Microsoft.OpenApi.Models;
using TeamFlow.Api.Auth;
using TeamFlow.Api.Middleware;
using TeamFlow.Api.Realtime;
using TeamFlow.Application;
using TeamFlow.Application.Common.Realtime;
using TeamFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder
    .Services.AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddSupabaseAuth(builder.Configuration);

builder.Services.AddSignalR();

// Replace the no-op publisher registered by AddApplication with the SignalR-backed one.
builder.Services.AddSingleton<IRealtimePublisher, SignalRRealtimePublisher>();

builder
    .Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()
        );
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "TeamFlow API", Version = "v1" });
    opt.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description = "Supabase JWT — paste the access token from the client.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        }
    );
    opt.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                }
            ] = Array.Empty<string>(),
        }
    );
});

builder.Services.AddProblemDetails();

builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p =>
    {
        var allowedOrigins =
            builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();

        p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    })
);

builder
    .Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Default") ?? "Host=localhost",
        name: "postgres"
    );

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<RealtimeHub>(RealtimeHub.Path);
app.MapHealthChecks("/health");

app.Run();
