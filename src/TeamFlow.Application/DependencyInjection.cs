using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Behaviors;
using TeamFlow.Application.Common.Realtime;

namespace TeamFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // Order matters: Logging → Validation → UnitOfWork → handler
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Scoped per-request buffer for realtime events. The transport-side publisher is
        // registered by the Api project (SignalR-backed); a no-op fallback lives here so
        // application tests don't need to wire SignalR.
        services.AddScoped<IRealtimePublishQueue, RealtimePublishQueue>();
        services.AddSingleton<IRealtimePublisher, NoOpRealtimePublisher>();

        return services;
    }
}
