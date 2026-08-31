using System.Reflection;
using Xunit;

namespace Notrelix.Architecture.Tests.LayerRules;

/// <summary>
/// ARCH-BC-008 — Common Semantic Leakage, initial anti-regression (Wave 3).
///
/// `Application/Common/Entitlements` is the known migration hotspot
/// (BOUND-COMMON-002): it carries Billing-owned plan/tier/subscription
/// vocabulary as pipeline-era shared abstractions. The hotspot may shrink only
/// through the governed entitlement migration; this gate prevents silent
/// growth:
///
///   1. the type set of Common.Entitlements is frozen — new types require the
///      governed migration to touch this test (no expansion by convenience);
///   2. product context code (Features/{Context}) must not import
///      Common.Entitlements — consumers speak capability semantics through
///      producer Public contracts or consumer ports, not tier/plan APIs.
///
/// This does not require removal of Common/Entitlements (CERTIFICATION on
/// anti-regression): untouched legacy may remain until its migration trigger.
/// </summary>
public class CommonEntitlementsAntiRegressionTests : ArchitectureTestBase
{
    /// <summary>
    /// Frozen hotspot inventory at boundary Wave 3 activation (candidate SHA
    /// ad068b74). Add an entry only together with the governed entitlement
    /// migration that justifies it.
    /// </summary>
    private static readonly IReadOnlySet<string> KnownEntitlementsTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "Notrelix.Application.Common.Entitlements.FeatureCode",
        "Notrelix.Application.Common.Entitlements.IEntitlementChecker",
        "Notrelix.Application.Common.Entitlements.IFeatureGateChecker",
        "Notrelix.Application.Common.Entitlements.ISubscriptionChecker",
    };

    [Fact]
    public void CommonEntitlements_TypeSet_MustNotGrowSilently()
    {
        var actualTypes = Assembly.Load("Notrelix.Application")
            .GetTypes()
            .Where(t => t.Namespace == "Notrelix.Application.Common.Entitlements")
            .Select(t => t.FullName!)
            .ToHashSet(StringComparer.Ordinal);

        var added = actualTypes.Except(KnownEntitlementsTypes).ToList();
        var removed = KnownEntitlementsTypes.Except(actualTypes).ToList();

        added.Should().BeEmpty(
            "ARCH-BC-008: Common/Entitlements is a frozen migration hotspot — new " +
            "plan/tier/subscription vocabulary must go through the governed entitlement " +
            "migration and update this baseline. Added: " + string.Join(", ", added));

        // Removals are progress (hotspot burn-down): report them so the baseline
        // is intentionally shrunk in the same change.
        removed.Should().BeEmpty(
            "ARCH-BC-008: hotspot shrank — shrink KnownEntitlementsTypes in this test " +
            "in the same change. Removed: " + string.Join(", ", removed));
    }

    [Fact]
    public void ProductContextCode_ShouldNotImport_CommonEntitlements()
    {
        var appPath = GetApplicationPath();
        var featureFiles = Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

        var violations = featureFiles
            .Where(f => File.ReadAllText(f).Contains(
                "Notrelix.Application.Common.Entitlements",
                StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(appPath, f).Replace('\\', '/'))
            .ToList();

        violations.Should().BeEmpty(
            "ARCH-BC-008: product contexts must not consume Common/Entitlements tier/plan " +
            "APIs. Use Billing-owned Public semantic decisions via the canonical pipeline " +
            "feature gate or a consumer port. Violations: " + string.Join(", ", violations));
    }

    // ------------------------------------------------------------------
    // Gate self-tests
    // ------------------------------------------------------------------

    [Fact]
    public void Gate_KnownBaseline_IsNotEmpty_AndPrecise()
    {
        KnownEntitlementsTypes.Should().NotBeEmpty();
        KnownEntitlementsTypes.Should().OnlyContain(t => t.StartsWith("Notrelix.Application.Common.Entitlements.", StringComparison.Ordinal));
    }
}
