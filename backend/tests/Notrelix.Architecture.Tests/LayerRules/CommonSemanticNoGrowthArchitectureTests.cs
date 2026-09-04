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
                // Shrunk to the webhook-signature verification seam when the
                // n8n provider client migrated to Integrations ownership
                // (AI-REF-004). The remaining types are inbound signature
                // verification, not the outbound provider surface.
                "Notrelix.Application.Common.Integrations.N8n.IN8nSignatureService",
                "Notrelix.Application.Common.Integrations.N8n.N8nSignatureService",
            },
        };

    private static readonly IReadOnlyList<string> KnownBusinessVocabularyTokens =
    [
        // Business vocabulary that must never become a new Common namespace.
        // TAC-GATE-006: reference packs may not introduce shared PlanTier /
        // Permission / WorkspaceRole / AccountStatus types for convenience.
        "PlanTier",
        "SubscriptionTier",
        "Entitlement",
        "Permission",
        "WorkspaceRole",
        "AccountStatus",
    ];

    /// <summary>
    /// Exact technical-pipeline types under Common that are NOT business
    /// vocabulary and therefore must not be booked as business-vocabulary debt.
    /// Each entry is a real exact type identity and is excluded from the
    /// vocabulary scan by exact full name only (no namespace/name wildcards).
    ///
    ///   Notrelix.Application.Common.Security.AccessPermissionRule — a pure
    ///   technical authorization-pipeline representation (TAC-M3C): a
    ///   deserialized permission-rule row shape (`record(int Priority, string
    ///   Effect)`) used by the AccessFacts/AccessPolicyEngine seam governed by
    ///   WG-REF-002. It describes pipeline rule rows, not new shared business
    ///   vocabulary. Its presence on the token-matched vocabulary set is an
    ///   artifact of the `Permission` token, not a semantic classification.
    /// </summary>
    private static readonly IReadOnlySet<string> TechnicalPipelineTypes =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Notrelix.Application.Common.Security.AccessPermissionRule",
        };

    /// <summary>
    /// Exact governed business-vocabulary debt allowed to exist under Common
    /// outside the frozen namespaces. Each entry is a real production type
    /// already registered as exact debt; new vocabulary types are rejected and
    /// a baseline entry that disappears must shrink this baseline in the same
    /// change (two-way exact semantics).
    ///
    ///   Notrelix.Application.Common.Requests.Security.IRequirePermission —
    ///   DEBT-COMMON-001: the request marker exposes the Governance-owned
    ///   PermissionAction vocabulary through its Action member; it may only be
    ///   removed together with the governed permission-seam migration
    ///   (TAC-GATE-022 shrink). Namespace aligned with its Requests/Security/
    ///   folder in the request-marker namespace-alignment change.
    ///
    /// AccessPermissionRule is intentionally NOT listed here — it is classified
    /// as an exact technical-pipeline exception in TechnicalPipelineTypes, not
    /// as business-vocabulary debt (TAC-M3C).
    /// </summary>
    private static readonly IReadOnlySet<string> KnownVocabularyDebt =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Notrelix.Application.Common.Requests.Security.IRequirePermission",
        };

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
        var flaggedDebt = CollectVocabularyDebt();

        var unexpected = flaggedDebt.Except(KnownVocabularyDebt).ToList();
        var stale = KnownVocabularyDebt.Except(flaggedDebt).ToList();

        unexpected.Should().BeEmpty(
            "ARCH-BC-008: reference packs must not push plan/role/permission/entitlement " +
            "vocabulary into Application/Common. A new business-vocabulary type requires " +
            "the governed migration that moves it to its owning context. Violations:\n" +
            string.Join("\n", unexpected));

        stale.Should().BeEmpty(
            "ARCH-BC-008: the vocabulary-debt baseline shrank — shrink KnownVocabularyDebt " +
            "in this test in the same change. Removed: " + string.Join(", ", stale));
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

    [Fact]
    public void Gate_Detects_NewVocabularyDebt_OutsideBaseline()
    {
        var flagged = new HashSet<string>(StringComparer.Ordinal)
        {
            "Notrelix.Application.Common.Requests.Security.IRequirePermission", // registered exact debt
            "Notrelix.Application.Common.Foo.WorkspaceRole", // synthetic new debt
        };

        var unexpected = flagged.Except(KnownVocabularyDebt).ToList();

        unexpected.Should().Contain("Notrelix.Application.Common.Foo.WorkspaceRole",
            "a new business-vocabulary Common type must fail — no broad token allowance");
        unexpected.Should().ContainSingle(
            "the exact registered debt entries must stay allowed");
    }

    // TAC-M3C self-tests — technical-pipeline exact classification.

    [Fact]
    public void Gate_TechPipelineTypes_AreExactCommonIdentities()
    {
        TechnicalPipelineTypes.Should().OnlyContain(
            t => t.StartsWith("Notrelix.Application.Common.", StringComparison.Ordinal),
            "technical-pipeline entries must be exact Common type identities, not wildcards");
    }

    [Fact]
    public void Gate_AccessPermissionRule_Is_TechnicalNotDebt()
    {
        // AccessPermissionRule must not be booked as business-vocabulary debt.
        KnownVocabularyDebt.Should().NotContain(
            "Notrelix.Application.Common.Security.AccessPermissionRule",
            "AccessPermissionRule is classified exactly once as a technical-pipeline exception (TAC-M3C)");

        TechnicalPipelineTypes.Should().Contain(
            "Notrelix.Application.Common.Security.AccessPermissionRule",
            "AccessPermissionRule is the exact technical-pipeline exception");
    }

    [Fact]
    public void Gate_Detects_NewPermissionVocabulary_NotMaskedAsTechnical()
    {
        // A NEW permission-vocabulary type (even under Security.*) must fail —
        // there is no Security.* / Permission* wildcard forgiveness. Exact
        // TechnicalPipelineTypes is the only exemption.
        var flagged = new HashSet<string>(StringComparer.Ordinal)
        {
            "Notrelix.Application.Common.Security.PermissionRule", // synthetic new business vocabulary
        };

        var unexpected = flagged.Except(TechnicalPipelineTypes).ToList();

        unexpected.Should().Contain("Notrelix.Application.Common.Security.PermissionRule",
            "a new permission/role vocabulary type must not ride the technical-pipeline exception");
    }

    [Fact]
    public void Gate_VocabularyDebt_IsExact()
    {
        KnownVocabularyDebt.Should().Contain(
            "Notrelix.Application.Common.Requests.Security.IRequirePermission",
            "DEBT-COMMON-001 is the only sanctioned permission-marker vocabulary-debt entry");

        KnownVocabularyDebt.Should().OnlyContain(t =>
            t.StartsWith("Notrelix.Application.Common.", StringComparison.Ordinal),
            "debt entries must be exact Common type identities, not namespace wildcards");
    }

    private static SortedSet<string> CollectVocabularyDebt()
    {
        var debt = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var type in Assembly.Load("Notrelix.Application").GetTypes())
        {
            if (type.Namespace?.StartsWith("Notrelix.Application.Common.", StringComparison.Ordinal) != true)
                continue;

            if (FrozenBusinessNamespaces.ContainsKey(type.Namespace))
                continue;

            // TAC-M3C: exact technical-pipeline types are excluded by exact full
            // name (not namespace/name wildcard). They are not business debt.
            if (TechnicalPipelineTypes.Contains(type.FullName!))
                continue;

            if (KnownBusinessVocabularyTokens.Any(token =>
                    type.Name.Contains(token, StringComparison.Ordinal)))
            {
                debt.Add(type.FullName!);
            }
        }

        return debt;
    }
}
