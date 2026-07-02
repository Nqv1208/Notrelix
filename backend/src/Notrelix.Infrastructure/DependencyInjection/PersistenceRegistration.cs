using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Events;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Infrastructure.Services;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Abstractions;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Application.Common.Abstractions.Rls;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Options;

using Notrelix.Infrastructure.Data.Platform;
using Notrelix.Infrastructure.Data.Product;
using Notrelix.Infrastructure.Data.Projection;
using Notrelix.Infrastructure.Data.Runtime;

namespace Notrelix.Infrastructure;

/// <summary>
/// EF Core, PostgreSQL, interceptors, outbox persistence and seed options.
/// Registers 4 split bounded-context DbContexts (Platform, Product, Projection, Infrastructure)
/// plus a TransactionDbContext that coordinates atomic saves across all four.
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

        // ─── 1. PlatformDbContext — Account, Identity, Workspace, Governance ───
        services.AddDbContext<PlatformDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
            options.AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>());
            options.UseNpgsql(connectionString, npg =>
            {
                npg.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npg.MigrationsHistoryTable("__EFMigrationsHistory_Platform", DbSchemas.Ops);
            }).UseSnakeCaseNamingConvention();
        });

        // ─── 2. ProductDbContext — WorkMgmt, Docs, Collab, Automation, Integrations, Billing ───
        services.AddDbContext<ProductDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
            options.AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>());
            options.UseNpgsql(connectionString, npg =>
            {
                npg.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npg.MigrationsHistoryTable("__EFMigrationsHistory_Product", DbSchemas.Ops);
            }).UseSnakeCaseNamingConvention();
        });

        // ─── 3. ProjectionDbContext — Search, Notifications, Activity ───
        services.AddDbContext<ProjectionDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
            options.UseNpgsql(connectionString, npg =>
            {
                npg.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npg.MigrationsHistoryTable("__EFMigrationsHistory_Projection", DbSchemas.Ops);
            }).UseSnakeCaseNamingConvention();
        });

        // ─── 4. InfrastructureDbContext — Events, Messaging, Audit, Ops ───
        services.AddDbContext<InfrastructureDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
            options.AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>());
            options.UseNpgsql(connectionString, npg =>
            {
                npg.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npg.MigrationsHistoryTable("__EFMigrationsHistory_Infrastructure", DbSchemas.Ops);
            }).UseSnakeCaseNamingConvention();
        });

        // ─── 5. Legacy ApplicationDbContext — migrations / design-time / tests ───
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
            options.AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>());
            options.UseNpgsql(connectionString, npgOptions =>
            {
                npgOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgOptions.MigrationsHistoryTable("__EFMigrationsHistory", DbSchemas.Ops);
            }).UseSnakeCaseNamingConvention();
        });

        // ─── IApplicationDbContext → TransactionDbContext (atomic cross-context save) ───
        services.AddScoped<IApplicationDbContext>(sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            var product = sp.GetRequiredService<ProductDbContext>();
            var projection = sp.GetRequiredService<ProjectionDbContext>();
            var infrastructure = sp.GetRequiredService<InfrastructureDbContext>();
            return new TransactionDbContext(platform, product, projection, infrastructure);
        });

        // ─── Map bounded-context interfaces → correct physical DbContext ───
        // Platform
        services.AddScoped<IAccountDbContext>(sp => sp.GetRequiredService<PlatformDbContext>());
        services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<PlatformDbContext>());
        services.AddScoped<IWorkspaceDbContext>(sp => sp.GetRequiredService<PlatformDbContext>());
        services.AddScoped<IGovernanceDbContext>(sp => sp.GetRequiredService<PlatformDbContext>());
        // Product
        services.AddScoped<IWorkManagementDbContext>(sp => sp.GetRequiredService<ProductDbContext>());
        services.AddScoped<IDocumentDbContext>(sp => sp.GetRequiredService<ProductDbContext>());
        services.AddScoped<ICollaborationDbContext>(sp => sp.GetRequiredService<ProductDbContext>());
        services.AddScoped<IAutomationDbContext>(sp => sp.GetRequiredService<ProductDbContext>());
        services.AddScoped<IIntegrationDbContext>(sp => sp.GetRequiredService<ProductDbContext>());
        services.AddScoped<IBillingDbContext>(sp => sp.GetRequiredService<ProductDbContext>());
        services.AddScoped<IReportingDbContext>(sp => sp.GetRequiredService<ProductDbContext>());
        // Projection
        services.AddScoped<ISearchProjectionDbContext>(sp => sp.GetRequiredService<ProjectionDbContext>());
        services.AddScoped<INotificationDbContext>(sp => sp.GetRequiredService<ProjectionDbContext>());
        services.AddScoped<IActivityProjectionDbContext>(sp => sp.GetRequiredService<ProjectionDbContext>());
        // Infrastructure
        services.AddScoped<IMessagingDbContext>(sp => sp.GetRequiredService<InfrastructureDbContext>());
        services.AddScoped<IAuditDbContext>(sp => sp.GetRequiredService<InfrastructureDbContext>());
        services.AddScoped<IOpsDbContext>(sp => sp.GetRequiredService<InfrastructureDbContext>());
        services.AddScoped<IWorkspaceAccessChecker, WorkspaceAccessChecker>();
        services.AddScoped<IWorkspaceAccessResolver, WorkspaceAccessResolver>();
        services.AddScoped<IActorLookupService, ActorLookupService>();
        services.AddScoped<IResourceReferenceResolver, ResourceReferenceResolver>();
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
