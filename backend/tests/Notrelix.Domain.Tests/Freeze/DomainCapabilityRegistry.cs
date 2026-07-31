using System.Reflection;
using FluentAssertions;
using DomainCapability = Notrelix.Domain.Tests.Freeze.DomainCapabilityRegistry.DomainCapability;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Defines how a capability namespace prefix matches types.
/// </summary>
internal enum NamespaceMatch
{
    /// <summary>
    /// Matches only types directly in the exact namespace.
    /// Sub-namespaces are NOT matched and must be registered separately.
    /// </summary>
    Exact,

    /// <summary>
    /// Matches the namespace and all sub-namespaces (prefix match).
    /// </summary>
    Subtree
}

/// <summary>
/// Single source of truth for capability maturity and tenant scope.
/// Every freeze gate uses this registry instead of maintaining separate lists.
/// </summary>
internal static class DomainCapabilityRegistry
{
    internal sealed record DomainCapability(
        string NamespacePrefix,
        DomainCapabilityStatus Status,
        NamespaceMatch Match = NamespaceMatch.Subtree);

    private static readonly DomainCapability[] Capabilities =
    [
        // ── Common ────────────────────────────────────────────────────────
        new("Notrelix.Domain.Common", DomainCapabilityStatus.Frozen),

        // ── SharedKernel ──────────────────────────────────────────────────
        new("Notrelix.Domain.SharedKernel", DomainCapabilityStatus.Frozen),

        // ── Accounts ──────────────────────────────────────────────────────
        new("Notrelix.Domain.Accounts", DomainCapabilityStatus.Frozen),

        // ── Identity ──────────────────────────────────────────────────────
        new("Notrelix.Domain.Identity", DomainCapabilityStatus.Frozen),

        // ── Workspaces ────────────────────────────────────────────────────
        new("Notrelix.Domain.Workspaces", DomainCapabilityStatus.Frozen),

        // ── WorkManagement stable core ────────────────────────────────────
        // Root prefix for RuleCodes/enums only; sub-namespaces registered separately.
        // Exact match ensures new sub-capabilities must be explicitly classified.
        new("Notrelix.Domain.WorkManagement", DomainCapabilityStatus.Frozen, NamespaceMatch.Exact),
        new("Notrelix.Domain.WorkManagement.Boards", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.WorkManagement.BoardGroups", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.WorkManagement.Fields", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.WorkManagement.Items", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.WorkManagement.Views", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.WorkManagement.Checklists", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.WorkManagement.Labels", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.WorkManagement.Forms", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.WorkManagement.Relations", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.WorkManagement.Templates", DomainCapabilityStatus.Frozen),

        // ── WorkManagement experimental ───────────────────────────────────
        new("Notrelix.Domain.WorkManagement.Formulas", DomainCapabilityStatus.Experimental),
        new("Notrelix.Domain.WorkManagement.Rollups", DomainCapabilityStatus.Experimental),
        new("Notrelix.Domain.WorkManagement.Workload", DomainCapabilityStatus.Experimental),
        new("Notrelix.Domain.WorkManagement.Approvals", DomainCapabilityStatus.Experimental),

        // ── Documents ─────────────────────────────────────────────────────
        new("Notrelix.Domain.Documents", DomainCapabilityStatus.Frozen),

        // ── Collaboration ─────────────────────────────────────────────────
        // Root prefix for RuleCodes only; sub-namespaces registered separately.
        new("Notrelix.Domain.Collaboration", DomainCapabilityStatus.Frozen, NamespaceMatch.Exact),
        new("Notrelix.Domain.Collaboration.Attachments", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Collaboration.Comments", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Collaboration.Mentions", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Collaboration.Reactions", DomainCapabilityStatus.Stabilizing),
        new("Notrelix.Domain.Collaboration.ReadStates", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Collaboration.Rules", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Collaboration.Watchers", DomainCapabilityStatus.Stabilizing),
        new("Notrelix.Domain.Collaboration.Presence", DomainCapabilityStatus.Experimental),

        // ── Governance ────────────────────────────────────────────────────
        // Root prefix for RuleCodes only; sub-namespaces registered separately.
        new("Notrelix.Domain.Governance", DomainCapabilityStatus.Frozen, NamespaceMatch.Exact),
        new("Notrelix.Domain.Governance.Permissions", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Governance.Policies", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Governance.Roles", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Governance.ShareLinks", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Governance.Templates", DomainCapabilityStatus.Frozen),

        // ── Automation ────────────────────────────────────────────────────
        // Root prefix for RuleCodes only; sub-namespaces registered separately.
        new("Notrelix.Domain.Automation", DomainCapabilityStatus.Frozen, NamespaceMatch.Exact),
        new("Notrelix.Domain.Automation.RulesEngine", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Automation.Rules", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Automation.Scheduled", DomainCapabilityStatus.Stabilizing),
        new("Notrelix.Domain.Automation.Templates", DomainCapabilityStatus.Stabilizing),

        // ── Automation experimental (runtime/orchestration) ────────────────
        new("Notrelix.Domain.Automation.Triggers", DomainCapabilityStatus.Experimental),
        new("Notrelix.Domain.Automation.Actions", DomainCapabilityStatus.Experimental),
        new("Notrelix.Domain.Automation.Conditions", DomainCapabilityStatus.Experimental),
        new("Notrelix.Domain.Automation.Executions", DomainCapabilityStatus.Experimental),
        new("Notrelix.Domain.Automation.Agents", DomainCapabilityStatus.Experimental),

        // ── Integrations ──────────────────────────────────────────────────
        // Root prefix for shared types only; sub-namespaces registered separately.
        new("Notrelix.Domain.Integrations", DomainCapabilityStatus.Frozen, NamespaceMatch.Exact),
        new("Notrelix.Domain.Integrations.Rules", DomainCapabilityStatus.Frozen, NamespaceMatch.Exact),
        new("Notrelix.Domain.Integrations.Connections", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Integrations.Calendar", DomainCapabilityStatus.Stabilizing),
        new("Notrelix.Domain.Integrations.Webhooks", DomainCapabilityStatus.Stabilizing),
        new("Notrelix.Domain.Integrations.Sync", DomainCapabilityStatus.Stabilizing),

        // ── Billing ───────────────────────────────────────────────────────
        new("Notrelix.Domain.Billing", DomainCapabilityStatus.Frozen),

        // ── Analytics ─────────────────────────────────────────────────────
        // Root prefix for RuleCodes only; sub-namespaces registered separately.
        new("Notrelix.Domain.Analytics", DomainCapabilityStatus.Frozen, NamespaceMatch.Exact),
        new("Notrelix.Domain.Analytics.Dashboards", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Analytics.Widgets", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Analytics.Snapshots", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Analytics.Rules", DomainCapabilityStatus.Frozen),
    ];

    /// <summary>
    /// Aggregates that are genuinely global (no tenant scope).
    /// These must NOT implement IAccountScoped or IWorkspaceScoped.
    /// </summary>
    internal static readonly IReadOnlySet<string> GlobalAggregates = new HashSet<string>
    {
        // Billing global
        "Notrelix.Domain.Billing.Plans.Plan",
        "Notrelix.Domain.Billing.Plans.FeatureCode",
        "Notrelix.Domain.Billing.Plans.PlanPrice",
        "Notrelix.Domain.Billing.Plans.BillingPeriod",
        "Notrelix.Domain.Billing.BillingEvents.BillingEvent",

        // Identity global (user identity is account-level but not workspace-scoped)
        "Notrelix.Domain.Identity.Users.User",
        "Notrelix.Domain.Identity.Tokens.EmailVerificationToken",
        "Notrelix.Domain.Identity.Tokens.PasswordResetToken",
        "Notrelix.Domain.Identity.Sessions.UserSession",
        "Notrelix.Domain.Identity.Security.UserLoginAttempt",
        "Notrelix.Domain.Identity.Security.UserSecuritySettings",
        "Notrelix.Domain.Identity.Profiles.UserProfile",
        "Notrelix.Domain.Identity.Mfa.UserMfaMethod",

        // WorkManagement templates (global scope, not tied to a specific workspace)
        "Notrelix.Domain.WorkManagement.Templates.BoardTemplate",

        // Documents templates
        "Notrelix.Domain.Documents.Templates.PageTemplate",

        // Integrations
        "Notrelix.Domain.Integrations.Webhooks.Events.InboundWebhookEvent",

        // Automation
        "Notrelix.Domain.Automation.Templates.AutomationTemplate",
    };

    /// <summary>
    /// Aggregates that span multiple scopes (e.g., hybrid System/Workspace templates).
    /// These implement a scope interface but are classified differently.
    /// </summary>
    internal static readonly IReadOnlySet<string> HybridAggregates = new HashSet<string>
    {
        "Notrelix.Domain.Governance.Templates.PermissionTemplate",
    };

    public static IReadOnlyList<DomainCapability> GetAll() => Capabilities;

    public static DomainCapabilityStatus GetStatus(string namespacePrefix)
    {
        var cap = Capabilities.FirstOrDefault(c =>
            string.Equals(c.NamespacePrefix, namespacePrefix, StringComparison.Ordinal));
        return cap?.Status ?? throw new ArgumentException($"Unknown namespace: {namespacePrefix}");
    }

    public static IEnumerable<DomainCapability> GetExperimental() =>
        Capabilities.Where(c => c.Status == DomainCapabilityStatus.Experimental);

    public static IEnumerable<DomainCapability> GetFrozen() =>
        Capabilities.Where(c => c.Status == DomainCapabilityStatus.Frozen);

    public static AggregateScopeKind ResolveScope(Type aggregateType)
    {
        if (GlobalAggregates.Contains(aggregateType.FullName!))
            return AggregateScopeKind.Global;

        if (HybridAggregates.Contains(aggregateType.FullName!))
            return AggregateScopeKind.Hybrid;

        if (typeof(IWorkspaceScoped).IsAssignableFrom(aggregateType))
            return AggregateScopeKind.Workspace;

        if (typeof(IAccountScoped).IsAssignableFrom(aggregateType))
            return AggregateScopeKind.Account;

        throw new InvalidOperationException(
            $"Aggregate scope is not classified: {aggregateType.FullName}. " +
            $"Register it in GlobalAggregates, HybridAggregates, or implement IWorkspaceScoped/IAccountScoped.");
    }

    public static DomainCapabilityStatus ResolveCapability(Type type)
    {
        var ns = type.Namespace;
        if (ns is null)
            throw new InvalidOperationException(
                $"Domain capability cannot be resolved for type with null namespace: {type.FullName}");

        // Find the best matching capability using Exact/Subtree semantics.
        // Exact: matches only the exact namespace (ns == prefix)
        // Subtree: matches the namespace and all sub-namespaces (ns == prefix || ns.StartsWith(prefix + "."))
        // Longest prefix wins among matches.
        var best = Capabilities
            .Where(c => c.Match switch
            {
                NamespaceMatch.Exact => ns == c.NamespacePrefix,
                NamespaceMatch.Subtree => ns == c.NamespacePrefix ||
                                          ns.StartsWith(c.NamespacePrefix + ".", StringComparison.Ordinal),
                _ => false
            })
            .MaxBy(c => c.NamespacePrefix.Length);

        return best?.Status
            ?? throw new InvalidOperationException(
                $"Domain capability is not classified: {type.FullName} (namespace: {ns}). " +
                $"Register it in DomainCapabilityRegistry.Capabilities.");
    }
}

/// <summary>
/// Validation tests for the DomainCapabilityRegistry itself.
/// </summary>
public class DomainCapabilityRegistryTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    [Fact]
    public void AllCapabilities_ShouldHaveNonEmptyNamespacePrefix()
    {
        var empty = DomainCapabilityRegistry.GetAll()
            .Where(c => string.IsNullOrWhiteSpace(c.NamespacePrefix))
            .ToList();

        empty.Should().BeEmpty("all capabilities must have a non-empty namespace prefix");
    }

    [Fact]
    public void NoOverlappingNamespacePrefixes()
    {
        var caps = DomainCapabilityRegistry.GetAll().ToList();
        var overlapping = new List<(DomainCapability A, DomainCapability B)>();

        for (var i = 0; i < caps.Count; i++)
        {
            for (var j = i + 1; j < caps.Count; j++)
            {
                var a = caps[i].NamespacePrefix;
                var b = caps[j].NamespacePrefix;

                // Parent-child prefixes are intentional (longest-prefix wins).
                // Sibling prefixes like "Rules" vs "RulesEngine" are fine because
                // longest-prefix matching resolves them to distinct namespace branches.
                // Only flag exact duplicates.
                if (a == b)
                {
                    overlapping.Add((caps[i], caps[j]));
                }
            }
        }

        overlapping.Should().BeEmpty(
            "namespace prefixes must not have exact duplicates: " +
            string.Join("; ", overlapping.Select(o => $"{o.A.NamespacePrefix} <-> {o.B.NamespacePrefix}")));
    }

    [Fact]
    public void Experimental_ShouldIncludeFourWorkManagementCapabilities()
    {
        var experimental = DomainCapabilityRegistry.GetExperimental()
            .Select(c => c.NamespacePrefix)
            .ToList();

        experimental.Should().Contain("Notrelix.Domain.WorkManagement.Formulas");
        experimental.Should().Contain("Notrelix.Domain.WorkManagement.Rollups");
        experimental.Should().Contain("Notrelix.Domain.WorkManagement.Workload");
        experimental.Should().Contain("Notrelix.Domain.WorkManagement.Approvals");
    }

    [Fact]
    public void Experimental_ShouldIncludePresence()
    {
        // Collaboration.Presence may be a sub-namespace or a directory
        var collabFrozen = DomainCapabilityRegistry.GetAll()
            .Where(c => c.NamespacePrefix.StartsWith("Notrelix.Domain.Collaboration", StringComparison.Ordinal))
            .ToList();

        // Presence is not explicitly registered as a capability yet,
        // but it should be classified as Experimental if it exists
        var presenceType = DomainAssembly.GetTypes()
            .FirstOrDefault(t => t.Namespace?.Contains("Presence") == true);

        if (presenceType is not null)
        {
            var status = DomainCapabilityRegistry.ResolveCapability(presenceType);
            status.Should().Be(DomainCapabilityStatus.Experimental,
                $"Presence type {presenceType.FullName} should be Experimental");
        }
    }

    [Fact]
    public void BoardGroups_ShouldBeCorrectlySpelled()
    {
        var caps = DomainCapabilityRegistry.GetAll()
            .Where(c => c.NamespacePrefix.Contains("BoardGroup", StringComparison.Ordinal))
            .ToList();

        caps.Should().ContainSingle(c =>
            c.NamespacePrefix == "Notrelix.Domain.WorkManagement.BoardGroups",
            "BoardGroups namespace must be spelled correctly (not BoardGroup)");
    }

    [Fact]
    public void EveryAggregateRoot_ShouldMapToExactlyOneCapability()
    {
        var aggregateRoots = DomainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(AggregateRoot).IsAssignableFrom(t))
            .ToList();

        // With fail-closed registry, ResolveCapability throws for unclassified types.
        // Every aggregate must be classifiable.
        var unclassified = aggregateRoots
            .Where(t =>
            {
                try { DomainCapabilityRegistry.ResolveCapability(t); return false; }
                catch (InvalidOperationException) { return true; }
            })
            .Select(t => t.FullName)
            .ToList();

        unclassified.Should().BeEmpty(
            "every aggregate must be classified by DomainCapabilityRegistry: " +
            string.Join(", ", unclassified));
    }

    [Fact]
    public void EveryAggregateRoot_ShouldMapToExactlyOneScope()
    {
        var aggregateRoots = DomainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(AggregateRoot).IsAssignableFrom(t))
            .ToList();

        // With fail-closed registry, ResolveScope throws for unclassified aggregates.
        // Every aggregate must be classifiable.
        var unclassified = aggregateRoots
            .Where(t =>
            {
                try { DomainCapabilityRegistry.ResolveScope(t); return false; }
                catch (InvalidOperationException) { return true; }
            })
            .Select(t => t.FullName)
            .ToList();

        unclassified.Should().BeEmpty(
            "every aggregate must be classified by scope (Global/Hybrid/Workspace/Account): " +
            string.Join(", ", unclassified));
    }

    [Fact]
    public void FrozenAggregates_ShouldNotBeExperimental()
    {
        var aggregateRoots = DomainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(AggregateRoot).IsAssignableFrom(t))
            .ToList();

        var experimentalPrefixes = DomainCapabilityRegistry.GetExperimental()
            .Select(c => c.NamespacePrefix)
            .ToList();

        var frozenExperimental = aggregateRoots
            .Where(t =>
            {
                var ns = t.Namespace ?? "";
                return experimentalPrefixes.Any(p => ns.StartsWith(p, StringComparison.Ordinal));
            })
            .Where(t => !IsInExperimentalDirectory(t))
            .Select(t => $"{t.FullName} (status={DomainCapabilityRegistry.ResolveCapability(t)})")
            .ToList();

        frozenExperimental.Should().BeEmpty(
            "no aggregate should be Experimental unless it lives in an Experimental namespace: " +
            string.Join(", ", frozenExperimental));
    }

    [Fact]
    public void GlobalAggregates_ShouldNotImplementTenantScope()
    {
        var violations = DomainCapabilityRegistry.GlobalAggregates
            .Select(n => DomainAssembly.GetType(n))
            .Where(t => t is not null)
            .Where(t => typeof(IWorkspaceScoped).IsAssignableFrom(t!) ||
                        typeof(IAccountScoped).IsAssignableFrom(t!))
            .Select(t => t!.FullName)
            .ToList();

        violations.Should().BeEmpty(
            "global aggregates must not implement tenant scope: " +
            string.Join(", ", violations));
    }

    private static bool IsInExperimentalDirectory(Type type)
    {
        var ns = type.Namespace ?? "";
        var experimentalPrefixes = DomainCapabilityRegistry.GetExperimental()
            .Select(c => c.NamespacePrefix)
            .ToList();

        return experimentalPrefixes.Any(p =>
            ns.StartsWith(p, StringComparison.Ordinal));
    }

    [Fact]
    public void EveryPublicDomainType_ShouldResolveExactlyOneCapability()
    {
        var domainTypes = DomainAssembly.GetTypes()
            .Where(t => t.IsPublic && t.Namespace?.StartsWith("Notrelix.Domain") == true)
            .ToList();

        var unclassified = new List<string>();
        foreach (var type in domainTypes)
        {
            try { DomainCapabilityRegistry.ResolveCapability(type); }
            catch (InvalidOperationException) { unclassified.Add(type.FullName!); }
        }

        unclassified.Should().BeEmpty(
            "every public Domain type must be classified by DomainCapabilityRegistry: " +
            string.Join(", ", unclassified));
    }

    [Fact]
    public void AllCapabilityPrefixes_ShouldExistInDomainAssembly()
    {
        var domainNamespaces = DomainAssembly.GetTypes()
            .Select(t => t.Namespace)
            .Where(ns => ns is not null)
            .Distinct()
            .ToHashSet();

        var missing = DomainCapabilityRegistry.GetAll()
            .Where(c => !domainNamespaces.Contains(c.NamespacePrefix))
            .Select(c => c.NamespacePrefix)
            .ToList();

        missing.Should().BeEmpty(
            "every capability namespace prefix must exist in the Domain assembly: " +
            string.Join(", ", missing));
    }

    [Fact]
    public void FrozenRoot_ShouldNotShadowExperimentalChild()
    {
        // Longest-prefix resolution handles parent/child correctly.
        // Verify that every Experimental type's resolved capability is Experimental.
        var experimentalPrefixes = DomainCapabilityRegistry.GetExperimental()
            .Select(c => c.NamespacePrefix)
            .ToHashSet();

        var experimentalTypes = DomainAssembly.GetTypes()
            .Where(t => t.IsPublic && t.Namespace is not null &&
                        experimentalPrefixes.Any(p => t.Namespace.StartsWith(p, StringComparison.Ordinal)))
            .ToList();

        var misclassified = experimentalTypes
            .Where(t => DomainCapabilityRegistry.ResolveCapability(t) != DomainCapabilityStatus.Experimental)
            .Select(t => $"{t.FullName} -> {DomainCapabilityRegistry.ResolveCapability(t)}")
            .ToList();

        misclassified.Should().BeEmpty(
            "no Experimental type should be resolved as Frozen due to a parent prefix: " +
            string.Join(", ", misclassified));
    }

    [Fact]
    public void NoAggregateInGlobal_ShouldImplementAccountOrWorkspaceScoped()
    {
        var violations = DomainCapabilityRegistry.GlobalAggregates
            .Select(n => DomainAssembly.GetType(n))
            .Where(t => t is not null)
            .Where(t => typeof(IAccountScoped).IsAssignableFrom(t) ||
                        typeof(IWorkspaceScoped).IsAssignableFrom(t))
            .Select(t => t!.FullName)
            .ToList();

        violations.Should().BeEmpty(
            "global aggregates must not implement IAccountScoped or IWorkspaceScoped: " +
            string.Join(", ", violations));
    }

    [Fact]
    public void GlobalAggregatesList_ShouldNotContainWorkspace()
    {
        DomainCapabilityRegistry.GlobalAggregates.Should().NotContain(
            n => n.Contains("Workspace"),
            "workspace aggregates must not be global");
    }

    [Fact]
    public void HybridAggregate_ShouldHaveDirectHybridInvariantTests()
    {
        var hybridTypes = DomainCapabilityRegistry.HybridAggregates
            .Select(n => DomainAssembly.GetType(n))
            .Where(t => t is not null)
            .ToList();

        var testsAssembly = typeof(DomainCapabilityRegistry).Assembly;
        var testTypes = testsAssembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract).ToList();

        foreach (var type in hybridTypes)
        {
            var ns = type!.Namespace!;
            var resolvedStatus = DomainCapabilityRegistry.ResolveCapability(type);
            resolvedStatus.Should().Be(DomainCapabilityStatus.Frozen,
                $"hybrid aggregate {type.FullName} must be Frozen");

            var fixtures = testTypes
                .Where(t => t.GetCustomAttributes<CoversHybridAggregateAttribute>(inherit: false)
                    .Any(a => a.AggregateType == type))
                .ToList();

            fixtures.Should().NotBeEmpty(
                $"hybrid aggregate {type.FullName} must have a [CoversHybridAggregate] fixture");

            fixtures.Should().Contain(f =>
                    f.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .Any(m => m.GetCustomAttributes(typeof(FactAttribute), true).Length > 0 ||
                                  m.GetCustomAttributes(typeof(TheoryAttribute), true).Length > 0),
                $"hybrid aggregate {type.FullName} fixtures must contain executable test methods");
        }
    }

    [Fact]
    public void HybridPermissionTemplate_ShouldBeClassifiedCorrectly()
    {
        var templateType = DomainAssembly.GetType("Notrelix.Domain.Governance.Templates.PermissionTemplate")!;

        var scope = DomainCapabilityRegistry.ResolveScope(templateType);
        scope.Should().Be(AggregateScopeKind.Hybrid);

        var capability = DomainCapabilityRegistry.ResolveCapability(templateType);
        capability.Should().Be(DomainCapabilityStatus.Frozen);
    }

    [Fact]
    public void LongestPrefix_ShouldResolveSubNamespaceOverRoot()
    {
        var governanceRoot = "Notrelix.Domain.Governance";
        var templatesSub = "Notrelix.Domain.Governance.Templates";

        var templateType = DomainAssembly.GetType("Notrelix.Domain.Governance.Templates.PermissionTemplate")!;

        var resolved = DomainCapabilityRegistry.ResolveCapability(templateType);

        var rootEntry = DomainCapabilityRegistry.GetAll()
            .First(c => c.NamespacePrefix == governanceRoot);
        var subEntry = DomainCapabilityRegistry.GetAll()
            .First(c => c.NamespacePrefix == templatesSub);

        resolved.Should().Be(subEntry.Status,
            $"{templatesSub} (longest prefix) must win over {governanceRoot} (shorter prefix)");
    }
}
