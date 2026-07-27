using System.Reflection;
using Notrelix.Domain.Billing.Common;
using Notrelix.Domain.Common;

namespace Notrelix.Architecture.Tests;

public class DomainHardeningArchitectureTests
{
    private static readonly string[] InfrastructureTerms =
    [
        "EfCore",
        "DbContext",
        "Redis",
        "MassTransit",
        "HttpClient",
        "Controller",
        "Endpoint",
        "Repository"
    ];

    private static readonly Dictionary<string, AllowlistEntry> InfrastructureTermAllowlist = new()
    {
        ["WebhookSubscription.cs:Endpoint"] = new(
            "WebhookSubscription",
            AllowlistClassification.FalsePositive,
            "Webhook endpoint is an integration-domain resource name, not an API endpoint.",
            "Keep provider/API endpoint implementations outside Domain."),
        ["WebhookDelivery.cs:Endpoint"] = new(
            "WebhookDelivery",
            AllowlistClassification.LegacyGap,
            "Webhook delivery is classified as operational delivery state by the Domain hardening rulebook.",
            "Move delivery retry/runtime state outside rich Domain or document a user-facing lifecycle before promotion.")
    };

    private static readonly Dictionary<string, AllowlistEntry> ProjectionClassificationAllowlist = new()
    {
        ["ReportingSnapshot"] = new(
            "ReportingSnapshot",
            AllowlistClassification.LegacyGap,
            "Analytics snapshots are classified as projections unless user-managed lifecycle is proven.",
            "Move to reporting projection or document aggregate lifecycle before promotion."),
        ["PresenceSession"] = new(
            "PresenceSession",
            AllowlistClassification.LegacyGap,
            "Presence is runtime connection state by default.",
            "Move to realtime/runtime infrastructure or document durable domain lifecycle."),
        ["PresenceUpdatedDomainEvent"] = new(
            "PresenceUpdatedDomainEvent",
            AllowlistClassification.LegacyGap,
            "Presence updates should not be durable domain events by default.",
            "Reclassify as realtime/runtime signal or document domain event need."),
        ["UnreadCounter"] = new(
            "UnreadCounter",
            AllowlistClassification.LegacyGap,
            "Unread counters are denormalized notification projections by default.",
            "Move to projection storage or document domain lifecycle."),
        ["MirrorValueSnapshot"] = new(
            "MirrorValueSnapshot",
            AllowlistClassification.LegacyGap,
            "Mirror snapshots are computed projections.",
            "Keep computation/projection outside aggregate behavior."),
        ["ItemParentSnapshot"] = new(
            "ItemParentSnapshot",
            AllowlistClassification.Intentional,
            "Application-supplied input data for Domain cycle-detection rules; not a persisted projection.",
            "Consider renaming to ItemParentChain if projection confusion persists."),
        ["ItemDependencySnapshot"] = new(
            "ItemDependencySnapshot",
            AllowlistClassification.Intentional,
            "Application-supplied input data for Domain cycle-detection rules; not a persisted projection.",
            "Consider renaming to ItemDependencyGraph if projection confusion persists."),
        ["OAuthProfileSnapshot"] = new(
            "OAuthProfileSnapshot",
            AllowlistClassification.Intentional,
            "Immutable ValueObject wrapping validated external OAuth profile data; not a runtime projection.",
            "None — this is a domain value object capturing external identity state."),
        ["ReportSnapshotPayload"] = new(
            "ReportSnapshotPayload",
            AllowlistClassification.Intentional,
            "Immutable ValueObject wrapping validated report payload data in the Analytics bounded context.",
            "None — this is a domain value object carrying typed report content.")
    };

    private static readonly Dictionary<string, AllowlistEntry> WorkspaceScopeAllowlist = new()
    {
        ["Workspace"] = new(
            "Workspace",
            AllowlistClassification.Intentional,
            "Workspace is the tenant root and must not implement IWorkspaceScoped.",
            "Keep Workspace as workspace root."),
        ["PermissionTemplate"] = new(
            "PermissionTemplate",
            AllowlistClassification.Intentional,
            "Permission templates may be global system templates when WorkspaceId is null.",
            "Keep template scope explicit in governance rulebook."),
        ["InboundWebhookEvent"] = new(
            "InboundWebhookEvent",
            AllowlistClassification.LegacyGap,
            "Inbound webhook events are provider/ops intake records with optional workspace metadata.",
            "Move provider intake/idempotency to infrastructure or document user-facing lifecycle."),
        ["ParentCommentContext"] = new(
            "ParentCommentContext",
            AllowlistClassification.Intentional,
            "Value object carrying WorkspaceId for comment parent resolution, not an independently persisted entity.",
            "Value objects use WorkspaceId for equality, not lifecycle."),
        ["BlockAncestorPath"] = new(
            "BlockAncestorPath",
            AllowlistClassification.Intentional,
            "Value object carrying WorkspaceId for block ancestor path resolution, not an independently persisted entity.",
            "Value objects use WorkspaceId for equality, not lifecycle."),
        ["WorkspaceRouteLinkedDomainEvent"] = new(
            "WorkspaceRouteLinkedDomainEvent",
            AllowlistClassification.Intentional,
            "Account-scoped event carrying non-nullable WorkspaceId only for the link operation. WorkspaceRoute is IAccountScoped with optional WorkspaceId.",
            "Keep as AccountScopedDomainEvent. The WorkspaceId is operation data, not lifecycle scope.")
    };

    private static readonly string[] CoreAggregates =
    [
        "User",
        "Workspace",
        "WorkspaceMember",
        "WorkspaceInvitation",
        "Board",
        "BoardItem",
        "BoardField",
        "Page",
        "Block",
        "Comment",
        "ResourcePermission",
        "CustomRole",
        "ShareLink",
        "Subscription",
        "Entitlement"
    ];

    [Fact]
    public void DomainNamespacesAndTypes_ShouldNotUse_InfrastructureTerms_WithoutClassification()
    {
        var violations = new List<string>();

        foreach (var type in GetDomainTypes())
        {
            foreach (var term in InfrastructureTerms)
            {
                if (!type.FullName!.Contains(term, StringComparison.Ordinal))
                    continue;

                var key = $"{Path.GetFileName(type.Assembly.Location)}:{term}";
                var sourceKey = $"{type.Name}.cs:{term}";
                if (InfrastructureTermAllowlist.ContainsKey(sourceKey) || InfrastructureTermAllowlist.ContainsKey(key))
                    continue;

                violations.Add($"{type.FullName} contains infrastructure/API term '{term}'");
            }
        }

        violations.Should().BeEmpty(
            "Domain namespaces and type names must not contain infrastructure/API terms unless classified. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void DomainTypes_WithRequiredWorkspaceId_ShouldImplement_IWorkspaceScoped_UnlessClassified()
    {
        var violations = new List<string>();

        foreach (var type in GetDomainTypes())
        {
            if (type.IsAbstract || type.IsEnum || type.IsInterface)
                continue;

            var workspaceIdProperty = type.GetProperty("WorkspaceId", BindingFlags.Public | BindingFlags.Instance);
            if (workspaceIdProperty == null || workspaceIdProperty.PropertyType != typeof(Guid))
                continue;

            if (typeof(IWorkspaceScoped).IsAssignableFrom(type))
                continue;

            if (WorkspaceScopeAllowlist.ContainsKey(type.Name))
                continue;

            violations.Add($"{type.FullName} has required WorkspaceId but does not implement IWorkspaceScoped");
        }

        violations.Should().BeEmpty(
            "Domain types with required WorkspaceId must implement IWorkspaceScoped " +
            "or be classified. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void DomainEvents_ShouldInherit_CorrectBaseClass()
    {
        var violations = new List<string>();

        foreach (var type in GetDomainTypes().Where(t => t is { IsClass: true, IsAbstract: false }
                                                   && typeof(IDomainEvent).IsAssignableFrom(t)
                                                   && typeof(DomainEvent).IsAssignableFrom(t)))
        {
            var inheritsScoped = typeof(WorkspaceScopedDomainEvent).IsAssignableFrom(type);
            var inheritsGlobal = typeof(GlobalDomainEvent).IsAssignableFrom(type);
            var inheritsBillingAccount = typeof(BillingAccountScopedDomainEvent).IsAssignableFrom(type);
            var inheritsAccount = typeof(AccountScopedDomainEvent).IsAssignableFrom(type);

            if (!inheritsScoped && !inheritsGlobal && !inheritsBillingAccount && !inheritsAccount)
            {
                violations.Add($"{type.FullName} inherits DomainEvent directly — should use scoped base");
            }
        }

        violations.Should().BeEmpty(
            "All concrete DomainEvents must inherit from GlobalDomainEvent, " +
            "WorkspaceScopedDomainEvent, AccountScopedDomainEvent, or BillingAccountScopedDomainEvent — not directly from DomainEvent. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void DomainEvents_ShouldFollowNamingAndBaseMetadataRules_UnlessClassified()
    {
        var violations = new List<string>();

        foreach (var type in GetDomainTypes().Where(t => t is { IsClass: true, IsAbstract: false }
                                                   && typeof(IDomainEvent).IsAssignableFrom(t)))
        {
            if (!typeof(DomainEvent).IsAssignableFrom(type))
            {
                violations.Add($"{type.FullName} implements IDomainEvent but does not inherit DomainEvent");
            }

            if (!type.Name.EndsWith("DomainEvent", StringComparison.Ordinal) &&
                !type.Name.EndsWith("Event", StringComparison.Ordinal))
            {
                violations.Add($"{type.FullName} must use a consistent event suffix");
            }

            var ctorHasOccurredAt = type.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Any(p => p.ParameterType == typeof(DateTimeOffset) &&
                          (p.Name?.Contains("At", StringComparison.OrdinalIgnoreCase) == true ||
                           p.Name?.Contains("occurred", StringComparison.OrdinalIgnoreCase) == true));

            if (!ctorHasOccurredAt)
            {
                violations.Add($"{type.FullName} must expose DateTimeOffset occurrence metadata through constructor parameters");
            }
        }

        violations.Should().BeEmpty(
            "Domain events must inherit DomainEvent, follow naming rules, and carry occurrence metadata. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void ProjectionRuntimeAndOpsTypes_ShouldBe_ClassifiedExplicitly()
    {
        var triggerTerms = new[] { "Snapshot", "Presence", "UnreadCounter", "Outbox", "Idempotency", "JobLock", "SearchDocument", "SearchIndex" };
        var violations = new List<string>();

        foreach (var type in GetDomainTypes())
        {
            if (type.IsAbstract || type.IsEnum || type.IsInterface)
                continue;

            if (!triggerTerms.Any(term => type.Name.Contains(term, StringComparison.Ordinal)))
                continue;

            if (ProjectionClassificationAllowlist.ContainsKey(type.Name))
                continue;

            if (type.Name is "DocumentSnapshot")
                continue;

            violations.Add($"{type.FullName} looks like projection/runtime/ops state and must be classified");
        }

        violations.Should().BeEmpty(
            "Projection/runtime/ops models must not become rich Domain types without explicit classification. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void CoreAggregateAudit_ShouldCover_EveryRequiredCoreAggregate()
    {
        var auditPath = Path.Combine(GetRepoRoot(), "docs", "domain", "core-aggregate-audit.md");

        File.Exists(auditPath).Should().BeTrue(
            "D2 requires docs/domain/core-aggregate-audit.md before production Domain hardening.");

        var content = File.ReadAllText(auditPath);
        var missing = CoreAggregates
            .Where(name => !content.Contains($"## Aggregate: {name}", StringComparison.Ordinal))
            .ToList();

        missing.Should().BeEmpty(
            "Every required core aggregate must have an audit entry before D5 production changes. " +
            $"Missing: {string.Join(", ", missing)}");
    }

    private static Type[] GetDomainTypes()
    {
        return typeof(Entity).Assembly.GetTypes();
    }

    private static string GetRepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }

        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");

        return current;
    }
}
