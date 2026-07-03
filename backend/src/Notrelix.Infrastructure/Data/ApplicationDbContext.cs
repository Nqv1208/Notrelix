using System.Reflection;
using System.Text.Json;
using System.Linq.Expressions;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Infrastructure.Data.Abstractions;

// Account

// Identity
using Notrelix.Domain.Identity.Tokens;

// Workspace

// Documents
using Notrelix.Domain.Documents.Versions;

// WorkManagement
using Notrelix.Domain.WorkManagement.Views;

// Collaboration

// Governance

// Automation

// Integrations
using Notrelix.Domain.Integrations.Sync;

// Billing
using Notrelix.Domain.Billing.Usage;

// Analytics

// Infrastructure projections & records

namespace Notrelix.Infrastructure.Data;

public partial class ApplicationDbContext : DbContext,
    IApplicationDbContext,
    IWorkspaceDbContext,
    IWorkManagementDbContext,
    IIdentityDbContext,
    IAccountDbContext,
    IDocumentDbContext,
    ICollaborationDbContext,
    IAutomationDbContext,
    IGovernanceDbContext,
    IIntegrationDbContext,
    IBillingDbContext,
    IReportingDbContext,
    ISearchProjectionDbContext,
    IMessagingDbContext,
    IAuditDbContext,
    IOpsDbContext,
    IActivityProjectionDbContext,
    INotificationDbContext
{
    private readonly ICurrentTenantContext? _tenant;

    private static readonly FieldInfo TenantField = typeof(ApplicationDbContext)
        .GetField("_tenant", BindingFlags.NonPublic | BindingFlags.Instance)!;

    protected ICurrentTenantContext? Tenant => _tenant;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantContext? tenant = null) : base(options)
    {
        _tenant = tenant;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.Ignore<DomainEvent>();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AggregateRoot).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(AggregateRoot.Version))
                    .HasColumnName("version")
                    .IsConcurrencyToken()
                    .HasDefaultValue(1L);
            }
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var isSoftDeletable = typeof(SoftDeletableEntity).IsAssignableFrom(entityType.ClrType);
            var isWorkspaceScoped = typeof(IWorkspaceScoped).IsAssignableFrom(entityType.ClrType);

            if (!isSoftDeletable && !isWorkspaceScoped)
                continue;

            var param = Expression.Parameter(entityType.ClrType, "e");
            Expression? filterBody = null;

            if (isSoftDeletable)
            {
                filterBody = Expression.Equal(
                    Expression.PropertyOrField(param, "DeletedAt"),
                    Expression.Constant(null, typeof(DateTimeOffset?)));
            }

            if (isWorkspaceScoped)
            {
                if (_tenant is null)
                {
                    // Null tenant → block all access (evaluated at model creation time).
                    var noAccess = Expression.Constant(false);
                    filterBody = filterBody is not null
                        ? Expression.AndAlso(filterBody, noAccess)
                        : noAccess;
                }
                else
                {
                    // Runtime filter evaluated at QUERY TIME via EF Core's funcletizer.
                    // Expression: tenant.IsSystemContext
                    //     || (e.AccountId == tenant.AccountId && e.WorkspaceId == tenant.WorkspaceId)

                    var contextField = Expression.Field(Expression.Constant(this), TenantField);
                    var isSysProp = Expression.Property(contextField, nameof(ICurrentTenantContext.IsSystemContext));
                    var wsIdProp = Expression.Property(contextField, nameof(ICurrentTenantContext.WorkspaceId));
                    var acctIdProp = Expression.Property(contextField, nameof(ICurrentTenantContext.AccountId));

                    var wsIdEquals = Expression.Equal(
                        Expression.Convert(Expression.PropertyOrField(param, "WorkspaceId"), typeof(Guid?)),
                        Expression.Convert(wsIdProp, typeof(Guid?)));

                    var acctIdEquals = Expression.Equal(
                        Expression.Convert(Expression.PropertyOrField(param, "AccountId"), typeof(Guid?)),
                        Expression.Convert(acctIdProp, typeof(Guid?)));

                    var tenantMatch = Expression.AndAlso(acctIdEquals, wsIdEquals);
                    var innerOr = Expression.OrElse(isSysProp, tenantMatch);

                    filterBody = filterBody is not null
                        ? Expression.AndAlso(filterBody, innerOr)
                        : innerOr;
                }
            }

            if (filterBody is not null)
            {
                var lambda = Expression.Lambda(filterBody, param);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(Entity.Id))
                    .ValueGeneratedNever();
            }
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(JsonValue))
                {
                    property.SetValueConverter(new Converters.JsonValueConverter());
                    property.SetColumnType("jsonb");
                }
                else if (property.ClrType == typeof(FractionalIndex))
                    property.SetValueConverter(new Converters.FractionalIndexConverter());
                else if (property.ClrType == typeof(SecretRef))
                    property.SetValueConverter(new Converters.SecretRefConverter());
                else if (property.ClrType == typeof(JsonDocument))
                    property.SetValueConverter(new Converters.JsonDocumentConverter());
                else if (property.ClrType == typeof(TokenHash))
                    property.SetValueConverter(new Converters.TokenHashConverter());
                else if (property.ClrType == typeof(DocumentSnapshot))
                    property.SetValueConverter(new Converters.DocumentSnapshotConverter());
                else if (property.ClrType == typeof(UsageMetricKey))
                    property.SetValueConverter(new Converters.UsageMetricKeyConverter());
                else if (property.ClrType == typeof(GroupRule))
                    property.SetValueConverter(new Converters.GroupRuleConverter());
                else if (property.ClrType == typeof(SyncCursorValue))
                    property.SetValueConverter(new Converters.SyncCursorValueConverter());
            }
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType.IsEnum)
                {
                    var converterType = typeof(EnumToStringConverter<>).MakeGenericType(property.ClrType);
                    var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                    property.SetValueConverter(converter);
                    property.SetMaxLength(50);
                }
            }
        }
    }
}
