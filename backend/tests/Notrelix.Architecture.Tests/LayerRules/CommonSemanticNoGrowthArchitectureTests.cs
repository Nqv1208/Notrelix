using System.Reflection;

namespace Notrelix.Architecture.Tests.LayerRules;

/// <summary>
/// ARCH-BC-008 — Common Semantic No-Growth (closure hardening).
///
/// The Entitlements hotspot has its own exact frozen baseline
/// (CommonEntitlementsAntiRegressionTests). This gate protects the rest of
/// Common from becoming a second semantic authority: known business-vocabulary
/// namespaces are frozen (no new types without the owning migration), and new
/// commercial/business-vocabulary type names under other Common namespaces
/// fail by default.
///
/// Canonical authorization machinery under Common/Security and
/// Common/Requests (AccessFacts, IRequirePermission, ...) is pipeline-owned
/// and governed by WG-REF-002/authorization docs; role strings inside it are
/// data, not new Common vocabulary. Technical Common remains allowed.
/// </summary>
public class CommonSemanticNoGrowthArchitectureTests
{
    /// <summary>
    /// Frozen business-vocabulary namespace baselines. Each entry is the exact
    /// type set at activation; growth or shrink requires the governed
    /// migration that moves that vocabulary to its owning context.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> FrozenBusinessNamespaces =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["Notrelix.Application.Common.Entitlements"] = new HashSet<string>(StringComparer.Ordinal)
            {
                "Notrelix.Application.Common.Entitlements.FeatureCode",
                "Notrelix.Application.Common.Entitlements.IEntitlementChecker",
                "Notrelix.Application.Common.Entitlements.IFeatureGateChecker",
                "Notrelix.Application.Common.Entitlements.ISubscriptionChecker",
            },
            ["Notrelix.Application.Common.Integrations.N8n"] = new HashSet<string>(StringComparer.Ordinal)
            {
                // Frozen at closure hardening; the AI-REF-004 provider-boundary
                // migration must shrink this exact set when it moves N8n
                // provider semantics under Integrations ownership.
                "Notrelix.Application.Common.Integrations.N8n.IN8nClient",
                "Notrelix.Application.Common.Integrations.N8n.N8nTriggerResult",
                "Notrelix.Application.Common.Integrations.N8n.IN8nSignatureService",
                "Notrelix.Application.Common.Integrations.N8n.N8nSignatureService",
            },
        };

    private static readonly IReadOnlyList<string> KnownBusinessVocabularyTokens =
    [
        // Business vocabulary that must never become a new Common namespace.
        "PlanTier",
        "SubscriptionTier",
        "Entitlement",
    ];

    [Fact]
    public void FrozenBusinessNamespaces_TypeSets_AreExact()
    {
        var appTypes = Assembly.Load("Notrelix.Application").GetTypes();
        var violations = new List<string>();

        foreach (var (ns, baseline) in FrozenBusinessNamespaces)
        {
            var actual = appTypes
                .Where(t => t.Namespace == ns)
                .Select(t => t.FullName!)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var added in actual.Except(baseline))
                violations.Add($"{ns}: new type '{added}' — use the governed migration to its owning context");
            foreach (var removed in baseline.Except(actual))
                violations.Add($"{ns}: removed type '{removed}' — shrink this baseline in the same change");
        }

        violations.Should().BeEmpty(
            "ARCH-BC-008: frozen business-vocabulary Common namespaces must match their exact " +
            "reviewed baseline. Violations:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void CommonTypes_MustNotCarry_BusinessVocabularyNames()
    {
        var violations = Assembly.Load("Notrelix.Application")
            .GetTypes()
            .Where(t => t.Namespace?.StartsWith("Notrelix.Application.Common.", StringComparison.Ordinal) == true)
            .Where(t => !FrozenBusinessNamespaces.ContainsKey(t.Namespace!))
            .Where(t => KnownBusinessVocabularyTokens.Any(token =>
                t.Name.Contains(token, StringComparison.Ordinal)))
            .Select(t => $"{t.FullName}: business vocabulary must live in its owning context")
            .ToList();

        violations.Should().BeEmpty(
            "ARCH-BC-008: reference packs must not push plan/role/permission/entitlement " +
            "vocabulary into Application/Common. Violations:\n" + string.Join("\n", violations));
    }

    // ------------------------------------------------------------------
    // Gate self-tests
    // ------------------------------------------------------------------

    [Fact]
    public void Gate_FrozenBaselines_ArePrecise()
    {
        foreach (var (ns, baseline) in FrozenBusinessNamespaces)
        {
            baseline.Should().NotBeEmpty($"{ns} baseline must list its exact types");
            baseline.Should().OnlyContain(t => t.StartsWith(ns + ".", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Gate_Detects_NewVocabularyType()
    {
        const string ns = "Notrelix.Application.Common.Entitlements";
        var baseline = FrozenBusinessNamespaces[ns];

        baseline.Contains(ns + ".FeatureCode").Should().BeTrue();
        baseline.Contains(ns + ".NewPlanTierType").Should().BeFalse(
            "an unbaselined type would fail the exact-set check");
    }
}
