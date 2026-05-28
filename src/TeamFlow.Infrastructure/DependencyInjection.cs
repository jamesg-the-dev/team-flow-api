using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Features.Me.Queries.ListMyWorkspaces;
using TeamFlow.Application.Features.Projects.Queries.ListProjects;
using TeamFlow.Application.Features.Tasks.Queries.GetProjectBoard;
using TeamFlow.Application.Features.Workspaces.Queries.ListWorkspaceInvites;
using TeamFlow.Application.Features.Workspaces.Queries.ListWorkspaceMembers;
using TeamFlow.Application.Features.Workspaces.Queries.ListWorkspaceTags;
using TeamFlow.Domain.Activity;
using TeamFlow.Domain.Attachments;
using TeamFlow.Domain.Discussions;
using TeamFlow.Domain.Notifications;
using TeamFlow.Domain.Projects;
using TeamFlow.Domain.SeedWork;
using TeamFlow.Domain.Tasks;
using TeamFlow.Domain.Workspaces;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Infrastructure.Persistence.Interceptors;
using TeamFlow.Infrastructure.Persistence.QueryServices;
using TeamFlow.Infrastructure.Persistence.Repositories;
using TeamFlow.Infrastructure.Services;

namespace TeamFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Missing ConnectionStrings:Default (Supabase Postgres)."
            );

        // Shared services
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IDomainEventDispatcher, MediatrDomainEventDispatcher>();
        services.AddScoped<AuditingInterceptor>();
        services.AddScoped<DomainEventDispatchInterceptor>();

        services.AddDbContext<TeamFlowDbContext>(
            (sp, options) =>
            {
                options.UseNpgsql(
                    connectionString,
                    npg =>
                    {
                        npg.EnableRetryOnFailure(3);
                        npg.MigrationsHistoryTable("__ef_migrations_history", "public");
                    }
                );
                options.UseSnakeCaseNamingConvention();
                options.AddInterceptors(
                    sp.GetRequiredService<AuditingInterceptor>(),
                    sp.GetRequiredService<DomainEventDispatchInterceptor>()
                );
            }
        );

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories — one per aggregate
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IChannelRepository, ChannelRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<IActivityEventRepository, ActivityEventRepository>();

        // Read-side query services (CQRS Q-side)
        services.AddScoped<IListProjectsQueryService, ListProjectsQueryService>();
        services.AddScoped<IGetProjectBoardQueryService, GetProjectBoardQueryService>();
        services.AddScoped<IListMyWorkspacesQueryService, ListMyWorkspacesQueryService>();
        services.AddScoped<IListWorkspaceMembersQueryService, ListWorkspaceMembersQueryService>();
        services.AddScoped<IListWorkspaceInvitesQueryService, ListWorkspaceInvitesQueryService>();
        services.AddScoped<IListWorkspaceTagsQueryService, ListWorkspaceTagsQueryService>();

        return services;
    }
}
