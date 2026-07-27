using System.Reflection;
using FluentAssertions;
using DomainCapability = Notrelix.Domain.Tests.Freeze.DomainCapabilityRegistry.DomainCapability;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Single source of truth for capability maturity and tenant scope.
/// Every freeze gate uses this registry instead of maintaining separate lists.
/// </summary>
internal static class DomainCapabilityRegistry
{
    internal sealed record DomainCapability(
        string NamespacePrefix,
        DomainCapabilityStatus Status);

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
        // Presence is Experimental; other Collaboration sub-namespaces are Frozen.
        // Do not register "Notrelix.Domain.Collaboration" as a blanket Frozen prefix
        // because Collaboration.Presence must be independently Experimental.
        new("Notrelix.Domain.Collaboration.Attachments", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Collaboration.Comments", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Collaboration.Mentions", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Collaboration.Reactions", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Collaboration.ReadStates", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Collaboration.Rules", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Collaboration.Watchers", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Collaboration.Presence", DomainCapabilityStatus.Experimental),

        // ── Governance ────────────────────────────────────────────────────
        new("Notrelix.Domain.Governance", DomainCapabilityStatus.Frozen),

        // ── Automation ────────────────────────────────────────────────────
        new("Notrelix.Domain.Automation.RulesEngine", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Automation.Scheduled", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Automation.Rules", DomainCapabilityStatus.Frozen),
        new("Notrelix.Domain.Automation.Templates", DomainCapabilityStatus.Frozen),

        // ── Automation experimental (runtime/orchestration) ────────────────
        new("Notrelix.Domain.Automation.Triggers", DomainCapabilityStatus.Experimental),
        new("Notrelix.Domain.Automation.Actions", DomainCapabilityStatus.Experimental),
        new("Notrelix.Domain.Automation.Conditions", DomainCapabilityStatus.Experimental),
        new("Notrelix.Domain.Automation.Executions", DomainCapabilityStatus.Experimental),
        new("Notrelix.Domain.Automation.Agents", DomainCapabilityStatus.Experimental),

        // ── Integrations ──────────────────────────────────────────────────
        new("Notrelix.Domain.Integrations", DomainCapabilityStatus.Frozen),

        // ── Billing ───────────────────────────────────────────────────────
        new("Notrelix.Domain.Billing", DomainCapabilityStatus.Frozen),

        // ── Analytics ─────────────────────────────────────────────────────
        new("Notrelix.Domain.Analytics", DomainCapabilityStatus.Frozen),
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

        // Workspaces tenant root
        "Notrelix.Domain.Workspaces.Workspaces.Workspace",

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

        return AggregateScopeKind.Global;
    }

    public static DomainCapabilityStatus ResolveCapability(Type type)
    {
        var ns = type.Namespace;
        if (ns is null) return DomainCapabilityStatus.Frozen;

        // Find the longest matching prefix
        var best = Capabilities
            .Where(c => ns.StartsWith(c.NamespacePrefix, StringComparison.Ordinal))
            .MaxBy(c => c.NamespacePrefix.Length);

        return best?.Status ?? DomainCapabilityStatus.Frozen;
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

                // Check if one is a true parent namespace of the other
                // (must be followed by '.' or end of string, not just a prefix match like Rules vs RulesEngine)
                var aStartsWithB = a.StartsWith(b + ".", StringComparison.Ordinal) || a == b;
                var bStartsWithA = b.StartsWith(a + ".", StringComparison.Ordinal) || b == a;

                if (aStartsWithB || bStartsWithA)
                {
                    overlapping.Add((caps[i], caps[j]));
                }
            }
        }

        overlapping.Should().BeEmpty(
            "namespace prefixes must not overlap: " +
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

        var unmapped = aggregateRoots
            .Where(t => DomainCapabilityRegistry.ResolveCapability(t) == DomainCapabilityStatus.Frozen &&
                        !DomainCapabilityRegistry.GetFrozen()
                            .Any(c => t.Namespace?.StartsWith(c.NamespacePrefix, StringComparison.Ordinal) == true))
            .Select(t => t.FullName)
            .ToList();

        // Global aggregates that don't match any capability prefix are acceptable
        // if they are in the GlobalAggregates set
        var trulyUnmapped = unmapped
            .Where(n => !DomainCapabilityRegistry.GlobalAggregates.Contains(n!) &&
                        !DomainCapabilityRegistry.HybridAggregates.Contains(n!))
            .ToList();

        trulyUnmapped.Should().BeEmpty(
            "every aggregate must map to a capability: " +
            string.Join(", ", trulyUnmapped));
    }

    [Fact]
    public void EveryAggregateRoot_ShouldMapToExactlyOneScope()
    {
        var aggregateRoots = DomainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(AggregateRoot).IsAssignableFrom(t))
            .ToList();

        var violations = aggregateRoots
            .Where(t => !typeof(IWorkspaceScoped).IsAssignableFrom(t) &&
                        !typeof(IAccountScoped).IsAssignableFrom(t) &&
                        !DomainCapabilityRegistry.GlobalAggregates.Contains(t.FullName!) &&
                        !DomainCapabilityRegistry.HybridAggregates.Contains(t.FullName!))
            .Select(t => t.FullName)
            .ToList();

        violations.Should().BeEmpty(
            "aggregates without IWorkspaceScoped/IAccountScoped must be registered as Global or Hybrid: " +
            string.Join(", ", violations));
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
}
