using System.Reflection;
using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Documents.Versions;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.WorkManagement.Views;
using Notrelix.Domain.Integrations.Sync;

namespace Notrelix.Infrastructure.Data;

/// <summary>
/// Shared EF Core model-building logic for all Notrelix bounded-context DbContexts.
/// Handles: soft-delete + workspace-scoped query filters, value converters,
/// version/concurrency tokens, Entity.Id ValueGeneratedNever, and enum→string conversion.
/// </summary>
public abstract class BaseNotrelixDbContext : DbContext
{
    private readonly ICurrentWorkspace? _currentWorkspace;

    private static readonly FieldInfo CurrentWorkspaceField = typeof(BaseNotrelixDbContext)
        .GetField("_currentWorkspace", BindingFlags.NonPublic | BindingFlags.Instance)!;

    protected ICurrentWorkspace? CurrentWorkspace => _currentWorkspace;

    protected BaseNotrelixDbContext(DbContextOptions options, ICurrentWorkspace? currentWorkspace = null)
        : base(options)
    {
        _currentWorkspace = currentWorkspace;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.Ignore<DomainEvent>();

        ApplyEntityConfigurations(modelBuilder);

        ApplyAggregateVersionTokens(modelBuilder);
        ApplyQueryFilters(modelBuilder);
        ApplyEntityIdNever(modelBuilder);
        ApplyValueConverters(modelBuilder);
        ApplyEnumConverters(modelBuilder);
    }

    /// <summary>
    /// Override in derived DbContexts to filter IEntityTypeConfiguration by namespace.
    /// </summary>
    protected virtual void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BaseNotrelixDbContext).Assembly);
    }

    private void ApplyAggregateVersionTokens(ModelBuilder modelBuilder)
    {
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
    }

    private void ApplyEntityIdNever(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(Entity.Id))
                    .ValueGeneratedNever();
            }
        }
    }

    private void ApplyQueryFilters(ModelBuilder modelBuilder)
    {
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
                if (_currentWorkspace is null)
                {
                    var noAccess = Expression.Constant(false);
                    filterBody = filterBody is not null
                        ? Expression.AndAlso(filterBody, noAccess)
                        : noAccess;
                }
                else
                {
                    var contextField = Expression.Field(Expression.Constant(this), CurrentWorkspaceField);
                    var isSysProp = Expression.Property(contextField, nameof(ICurrentWorkspace.IsSystemContext));
                    var wsIdProp = Expression.Property(contextField, nameof(ICurrentWorkspace.WorkspaceId));
                    var acctIdProp = Expression.Property(contextField, nameof(ICurrentWorkspace.AccountId));

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
    }

    private static void ApplyValueConverters(ModelBuilder modelBuilder)
    {
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
    }

    private static void ApplyEnumConverters(ModelBuilder modelBuilder)
    {
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
