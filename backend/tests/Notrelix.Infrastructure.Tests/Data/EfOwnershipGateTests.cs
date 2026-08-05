using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Analytics;
using Notrelix.Infrastructure.Data.Audit;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Events;
using Notrelix.Infrastructure.Data.Governance.Projections;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Data.Notifications;
using Notrelix.Infrastructure.Data.Ops.Entities;
using Notrelix.Infrastructure.Data.Projections.Activity;
using Notrelix.Infrastructure.Data.Projections.Search;
using Notrelix.Infrastructure.Operations.Idempotency;

namespace Notrelix.Infrastructure.Tests.Data;

/// <summary>
/// Actual-model EF ownership gate (FZ-INF-01).
///
/// Technical ownership map follows 03-design-contracts-and-migration-matrix.md §4.
/// Every entity in the real ApplicationDbContext model must have exactly one owner:
///   - technical owners for the Infrastructure persistence records listed in the matrix;
///   - a business owner derived from its Domain namespace for everything else.
///
/// Assertions:
///   1. every entity has one owner (no unregistered/unclassified type);
///   2. a business entity appears in exactly one business context port, matching its owner;
///   3. technical entities are not exposed by business ports;
///   4. no cross-owner navigation/cascade in the EF model;
///   5. no owned business type shared across owners (SharedKernel value objects are exempt by design).
/// </summary>
public sealed class EfOwnershipGateTests
{
    private static readonly Dictionary<Type, string> TechnicalOwners = new()
    {
        // Operations
        [typeof(IdempotencyRecord)] = "Operations",
        [typeof(ImportJobRecord)] = "Operations",
        [typeof(ExportJobRecord)] = "Operations",
        [typeof(JobLockRecord)] = "Operations",

        // Messaging
        [typeof(MessagingOutboxMessage)] = "Messaging",
        [typeof(OutboxDeliveryAttempt)] = "Messaging",
        [typeof(MessagingProcessedEvent)] = "Messaging",
        [typeof(DomainEventLog)] = "Messaging",

        // Notifications delivery
        [typeof(EmailOutboxMessage)] = "NotificationsDelivery",
        [typeof(EmailDeliveryAttempt)] = "NotificationsDelivery",

        // Audit
        [typeof(AuditLog)] = "Audit",
        [typeof(SecurityEvent)] = "Audit",

        // ReadModels/Search
        [typeof(SearchDocumentRecord)] = "ReadModels/Search",
        [typeof(SearchIndexJobRecord)] = "ReadModels/Search",

        // ReadModels/Notifications
        [typeof(NotificationItemRecord)] = "ReadModels/Notifications",
        [typeof(NotificationRecipientRecord)] = "ReadModels/Notifications",
        [typeof(NotificationPreferenceRecord)] = "ReadModels/Notifications",
        [typeof(NotificationCounterRecord)] = "ReadModels/Notifications",

        // ReadModels/Activity
        [typeof(WorkspaceActivityLogRecord)] = "ReadModels/Activity",
        [typeof(ActivityReadStateRecord)] = "ReadModels/Activity",

        // ReadModels/Analytics
        [typeof(WorkspaceUsageDaily)] = "ReadModels/Analytics",
        [typeof(FeatureUsageDaily)] = "ReadModels/Analytics",

        // Authz technical projection
        [typeof(ResourcePermissionInheritanceCacheEntry)] = "AuthzProjection",
        [typeof(AccessGrant)] = "AuthzProjection",
    };

    /// <summary>Domain/Application namespace segment to the business context port exposing it.</summary>
    private static readonly Dictionary<string, string> BusinessContextToPort = new()
    {
        ["Accounts"] = "IAccountDbContext",
        ["Automation"] = "IAutomationDbContext",
        ["Billing"] = "IBillingDbContext",
        ["Collaboration"] = "ICollaborationDbContext",
        ["Documents"] = "IDocumentDbContext",
        ["Governance"] = "IGovernanceDbContext",
        ["Identity"] = "IIdentityDbContext",
        ["Integrations"] = "IIntegrationDbContext",
        ["Analytics"] = "IReportingDbContext",
        ["WorkManagement"] = "IWorkManagementDbContext",
        ["Workspaces"] = "IWorkspaceDbContext",
    };

    private const string SharedKernelOwner = "SharedKernel";

    private static readonly Lazy<Dictionary<string, HashSet<Type>>> BusinessPorts = new(DiscoverBusinessPorts);

    private static IModel BuildModel()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=dummy;Username=u;Password=p")
            .Options;
        using var context = new ApplicationDbContext(options);
        return context.Model;
    }

    private static Dictionary<string, HashSet<Type>> DiscoverBusinessPorts()
    {
        var applicationAssembly = typeof(Notrelix.Application.Features.WorkManagement.Abstractions.IWorkManagementDbContext).Assembly;
        var ports = new Dictionary<string, HashSet<Type>>();

        foreach (var port in applicationAssembly.GetTypes().Where(IsBusinessPort))
        {
            var types = port.GetProperties()
                .Where(p => p.PropertyType.IsGenericType
                         && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(p => p.PropertyType.GetGenericArguments()[0])
                .ToHashSet();
            ports[port.Name] = types;
        }

        return ports;
    }

    private static bool IsBusinessPort(Type type) =>
        type.IsInterface
        && type.Name.StartsWith('I')
        && type.Name.EndsWith("DbContext", StringComparison.Ordinal)
        && type.Namespace is not null
        && type.Namespace.StartsWith("Notrelix.Application.Features", StringComparison.Ordinal)
        && type.Namespace.EndsWith(".Abstractions", StringComparison.Ordinal);

    private static string? GetOwner(Type type)
    {
        if (TechnicalOwners.TryGetValue(type, out var technicalOwner))
            return technicalOwner;

        var ns = type.Namespace;
        if (ns is null) return null;

        if (ns.StartsWith("Notrelix.Domain.SharedKernel", StringComparison.Ordinal))
            return SharedKernelOwner;

        if (ns.StartsWith("Notrelix.Domain.", StringComparison.Ordinal))
            return "Business:" + ns.Substring("Notrelix.Domain.".Length).Split('.')[0];

        if (ns.StartsWith("Notrelix.Application.Features.", StringComparison.Ordinal))
            return "Business:" + ns.Substring("Notrelix.Application.Features.".Length).Split('.')[0];

        return null;
    }

    private static bool IsShared(string? owner) => owner == SharedKernelOwner;

    private static bool IsBusiness(string? owner) => owner?.StartsWith("Business:", StringComparison.Ordinal) == true;

    [Fact]
    public void EveryEntity_ShouldHaveExactlyOneOwner()
    {
        var model = BuildModel();
        var violations = new List<string>();

        foreach (var entity in model.GetEntityTypes())
        {
            var owner = GetOwner(entity.ClrType);
            if (owner is null)
                violations.Add($"{entity.ClrType.FullName} has no owner: add it to TechnicalOwners or move it into Notrelix.Domain.<Context>");
        }

        violations.Should().BeEmpty($"Every entity must have exactly one owner. {string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void BusinessEntity_ShouldNotAppearInMoreThanOneBusinessContextPort()
    {
        // An entity may legitimately appear in zero ports (owned child types, entities managed
        // exclusively by Infrastructure such as seed/initialiser). The defect this gate protects
        // against is cross-context exposure: one entity surfaced by two or more business ports,
        // or surfaced by a port that is not its own context (e.g. AutomationExecution previously
        // exposed by both IAutomationDbContext and IIntegrationDbContext).
        var model = BuildModel();
        var violations = new List<string>();

        foreach (var entity in model.GetEntityTypes())
        {
            var owner = GetOwner(entity.ClrType);
            if (!IsBusiness(owner)) continue;

            var context = owner!.Substring("Business:".Length);
            var expectedPort = BusinessContextToPort[context];

            var exposingPorts = BusinessPorts.Value
                .Where(kvp => kvp.Value.Contains(entity.ClrType))
                .Select(kvp => kvp.Key)
                .OrderBy(p => p)
                .ToList();

            if (exposingPorts.Count > 1 || (exposingPorts.Count == 1 && exposingPorts[0] != expectedPort))
                violations.Add($"{entity.ClrType.FullName}: expected at most [{expectedPort}] but found [{string.Join(", ", exposingPorts)}]");
        }

        violations.Should().BeEmpty(
            $"A business entity must appear in at most one business context port, its own. {string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void TechnicalEntities_ShouldNotBeExposedByBusinessPorts()
    {
        var violations = new List<string>();

        foreach (var (portName, types) in BusinessPorts.Value)
        {
            foreach (var type in types)
            {
                var owner = GetOwner(type);
                if (owner is null || !IsBusiness(owner))
                    violations.Add($"{portName}.{type.Name} exposes non-business entity owned by '{owner ?? "unknown"}'");
            }
        }

        violations.Should().BeEmpty(
            $"Technical entities must not be exposed by business context ports. {string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void Model_ShouldHaveNoCrossOwnerNavigationOrCascade()
    {
        var model = BuildModel();
        var violations = new List<string>();

        foreach (var entity in model.GetEntityTypes())
        {
            var owner = GetOwner(entity.ClrType);
            foreach (var navigation in entity.GetNavigations())
            {
                var targetOwner = GetOwner(navigation.TargetEntityType.ClrType);
                if (owner == targetOwner) continue;
                if (IsShared(owner) || IsShared(targetOwner)) continue;

                violations.Add($"{entity.ClrType.Name}.{navigation.Name} -> {navigation.TargetEntityType.ClrType.Name} " +
                               $"({owner} -> {targetOwner}, {navigation.ForeignKey.DeleteBehavior})");
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                if (foreignKey.PrincipalEntityType == entity) continue;
                var principalOwner = GetOwner(foreignKey.PrincipalEntityType.ClrType);
                if (owner == principalOwner) continue;
                if (IsShared(owner) || IsShared(principalOwner)) continue;

                violations.Add($"{entity.ClrType.Name} FK -> {foreignKey.PrincipalEntityType.ClrType.Name} " +
                               $"({owner} -> {principalOwner}, {foreignKey.DeleteBehavior})");
            }
        }

        violations.Should().BeEmpty(
            $"No navigation or cascade may cross owner boundaries. {string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void OwnedBusinessTypes_ShouldNotBeSharedAcrossOwners()
    {
        var model = BuildModel();
        var violations = new List<string>();

        foreach (var owned in model.GetEntityTypes().Where(e => e.IsOwned()))
        {
            var owner = GetOwner(owned.ClrType);
            if (IsShared(owner)) continue;

            var declaringOwners = owned.GetForeignKeys()
                .Select(fk => GetOwner(fk.PrincipalEntityType.ClrType))
                .Distinct()
                .ToList();

            var allOwners = declaringOwners.Append(owner).Distinct().ToList();
            if (allOwners.Count > 1)
                violations.Add($"{owned.ClrType.FullName} owned by [{string.Join(", ", allOwners)}]");
        }

        violations.Should().BeEmpty(
            $"No owned business type may be shared across owners. {string.Join(Environment.NewLine, violations)}");
    }
}
