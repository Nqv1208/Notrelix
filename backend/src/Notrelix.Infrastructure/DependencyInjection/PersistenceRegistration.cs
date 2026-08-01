using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Services;
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
using Notrelix.Infrastructure.Data.Services;
using Notrelix.Infrastructure.Data.Abstractions;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Options;

namespace Notrelix.Infrastructure;

/// <summary>
/// EF Core, PostgreSQL, interceptors, outbox persistence and seed options.
/// Single ApplicationDbContext maps all bounded-context interfaces.
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

        services.AddSingleton<IValidateOptions<RlsOptions>, RlsOptionsValidator>();

        // Interceptors (resolved inside AddDbContext below).
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DomainEventInterceptor>();

        var connectionString = configuration.GetConnectionString("NotrelixDb");

        // Single unified ApplicationDbContext
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
            options.AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>());
            options.UseNpgsql(connectionString, npg =>
            {
                npg.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npg.MigrationsHistoryTable("__EFMigrationsHistory", DbSchemas.Ops);
            }).UseSnakeCaseNamingConvention();
        });

        // IApplicationDbContext maps to ApplicationDbContext
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Provider-independent data session (transaction/RLS/SaveChanges mechanics)
        services.AddScoped<IRequestDataSession, EfRequestDataSession>();

        // Map bounded-context interfaces to ApplicationDbContext
        // Platform
        services.AddScoped<IAccountDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IWorkspaceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IGovernanceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        // Product
        services.AddScoped<IWorkManagementDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IDocumentDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ICollaborationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAutomationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IIntegrationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IBillingDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IReportingDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        // Projection
        services.AddScoped<ISearchProjectionDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<INotificationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IActivityProjectionDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        // Infrastructure
        services.AddScoped<IMessagingDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAuditDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IOpsDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IWorkspaceAccessChecker, WorkspaceAccessChecker>();
        services.AddScoped<IWorkspaceAccessResolver, WorkspaceAccessResolver>();
        services.AddScoped<IAccountAccessEvaluator, AccountAccessEvaluator>();
        services.AddScoped<ITenantBootstrapStore, TenantBootstrapStore>();
        services.AddScoped<IResourceScopeResolver, ResourceScopeResolver>();
        services.AddScoped<IActorLookupService, ActorLookupService>();
        services.AddScoped<IResourceReferenceResolver, ResourceReferenceResolver>();
        services.AddScoped<IResourceVersionReader, ResourceVersionReader>();
        services.AddScoped<ApplicationDbContextInitialiser>();
        services.AddScoped<RlsPolicyApplier>();
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();

        services.AddScoped<IDateTimeProvider, DateTimeProvider>();

        // Application services (ports in Application, adapters in Infrastructure)
        services.AddScoped<IIdentityUserLookupService, IdentityUserLookupService>();
        services.AddScoped<IAccountMembershipProvisioner, AccountMembershipProvisioner>();
        services.AddScoped<IAccountStatusReader, AccountStatusReader>();

        // Outbox persistence infrastructure.
        services.AddSingleton<IEventTypeRegistry, Notrelix.Infrastructure.Messaging.EventTypeRegistry>();
        services.AddSingleton<IClassificationPolicy>(_ =>
            ClassificationPolicy.CreateBuilder()
                .Build());
        services.AddSingleton<IDeliveryPolicy>(_ =>
            DeliveryPolicy.CreateBuilder()
                .Build());

        return services;
    }
}
