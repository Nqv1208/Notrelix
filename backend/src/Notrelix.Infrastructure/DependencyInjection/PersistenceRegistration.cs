using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Events;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Infrastructure.Services;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Application.Common.Abstractions.Rls;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Options;

namespace Notrelix.Infrastructure;

/// <summary>
/// EF Core, PostgreSQL, interceptors, outbox persistence and seed options.
/// </summary>
public static class PersistenceRegistration
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SeedDataOptions>()
            .Bind(configuration.GetSection("SeedData"))
            .Validate(o => o.Profile is SeedProfile.Small or SeedProfile.Medium or SeedProfile.Large,
                "SeedData:Profile must be Small, Medium, or Large.")
            .ValidateOnStart();

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection("Database"))
            .ValidateOnStart();

        services.AddOptions<RlsOptions>()
            .Bind(configuration.GetSection("Rls"))
            .ValidateOnStart();

        // Interceptors (resolved inside AddDbContext below).
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DomainEventInterceptor>();

        var connectionString = configuration.GetConnectionString("NotrelixDb");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
            options.AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>());
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

            options.UseNpgsql(connectionString, npgOptions =>
            {
                npgOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgOptions.MigrationsHistoryTable("__EFMigrationsHistory", DbSchemas.Ops);
            })
              .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IWorkspaceDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IWorkManagementDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IIdentityDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAccountDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IWorkspaceAccessChecker, WorkspaceAccessChecker>();
        services.AddScoped<ApplicationDbContextInitialiser>();
        services.AddScoped<RlsPolicyApplier>();
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();

        services.AddScoped<IDateTimeProvider, DateTimeProvider>();

        // Outbox persistence infrastructure.
        services.AddSingleton<IEventTypeRegistry, Notrelix.Infrastructure.Messaging.EventTypeRegistry>();
        services.AddSingleton<IDomainEventDispatchPolicy, DomainEventDispatchPolicy>();

        return services;
    }
}
