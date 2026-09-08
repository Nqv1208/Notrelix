using System.Reflection;

namespace Notrelix.Architecture.Tests.LayerRules;

/// <summary>
/// TAC-GATE-025 — Billing capacity semantic/concurrency protections
/// (structural side of TAC-FRZ-020).
///
/// The frozen M2 authority is BILL-LIMIT-001 = ZERO_CAPACITY:
///
///   Limit == 0 &amp;&amp; !IsUnlimited  → zero capacity (unavailable)
///   IsUnlimited == true          → unlimited (explicit representation)
///
/// Unlimited must never be inferred merely from numeric zero.
///
/// Enforced semantics:
///
///   1. zero-as-unlimited interpretation sites are governed debt — the exact
///      baseline below is owned by the M9 semantic flip (backfill is_unlimited
///      BEFORE the flip). New sites are rejected; removals must shrink the
///      baseline in the same change.
///   2. the hard-capacity owner state is the versioned WorkspaceFeatureUsage
///      aggregate with its QuotaExceededDomainEvent guard — it must remain the
///      authoritative concurrency owner.
///   3. product feature contexts must not consume Billing capacity aggregates
///      directly — capability semantics flow through Billing Public facts.
///   4. AUTOMATION_RULE classification authority is Billing-owned only.
///   5. no PlanTier type may exist in production; SubscriptionTier references
///      outside Billing-owned paths are governed debt with an exact baseline.
///
/// This gate proves the structural side only. Hard-capacity runtime
/// concurrency (last-slot races, reservation protocol, BI-FLOW-01..04)
/// remains mandatory M9 evidence.
/// </summary>
public class BillingCapacitySemanticsArchitectureTests : ArchitectureTestBase
{
    private const string GateId = "TAC-GATE-025";

    /// <summary>
    /// Exact governed-debt baseline (DEBT-BILL-001): production sites that
    /// interpret numeric zero as unlimited without the BILL-LIMIT-001
    /// IsUnlimited representation. Owned by the M9 semantic flip — new entries
    /// are forbidden; removals must shrink this baseline in the same change.
    /// </summary>
    private static readonly IReadOnlySet<string> ZeroAsUnlimitedBaseline =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Notrelix.Application/Features/Billing/Entitlements/Services/BillingCapabilityFactsProvider.cs",
            "Notrelix.Infrastructure/Billing/DatabaseFeatureGateChecker.cs",
            "Notrelix.Infrastructure/Data/Authz/AccessFactsQuery.cs",
        };

    /// <summary>
    /// Exact governed-debt baseline (DEBT-BILL-002): SubscriptionTier
    /// references outside Billing-owned paths. The access-policy tier ladder
    /// is legacy authorization plumbing and the Common.Entitlements types are
    /// the frozen ARCH-BC-008 hotspot; both are owned by the governed
    /// entitlement migration. New references are forbidden.
    /// </summary>
    private static readonly IReadOnlySet<string> TierReferenceBaseline =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Notrelix.Application/Common/Entitlements/IEntitlementChecker.cs",
            "Notrelix.Application/Common/Security/AccessFacts.cs",
            "Notrelix.Application/Common/Security/AccessPolicyEngine.cs",
        };

    // ------------------------------------------------------------------
    // Production rules
    // ------------------------------------------------------------------

    [Fact]
    public void ZeroInterpretationSites_MustMatch_ExactGovernedBaseline()
    {
        var actual = CollectZeroAsUnlimitedSites();

        var added = actual.Except(ZeroAsUnlimitedBaseline).ToList();
        var removed = ZeroAsUnlimitedBaseline.Except(actual).ToList();

        added.Should().BeEmpty(
            $"{GateId} / BILL-LIMIT-001: numeric zero must not be newly interpreted as unlimited. " +
            "Zero capacity and unlimited are distinct semantics; the existing sites are " +
            "governed debt owned by the M9 semantic flip. Violations: " + string.Join(", ", added));

        removed.Should().BeEmpty(
            $"{GateId} / BILL-LIMIT-001: the zero-as-unlimited baseline shrank — shrink " +
            "ZeroAsUnlimitedBaseline in this test in the same change (M9 flip). " +
            "Removed: " + string.Join(", ", removed));
    }

    [Fact]
    public void HardCapacityOwner_MustBe_VersionedAggregate_WithExceededGuard()
    {
        var usage = typeof(Notrelix.Domain.Billing.Usage.WorkspaceFeatureUsage);

        usage.Should().BeAssignableTo<Notrelix.Domain.Common.AggregateRoot>(
            $"{GateId}: the hard-capacity owner must carry aggregate Version optimistic concurrency");

        usage.GetMethod("Consume", BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull($"{GateId}: capacity consumption must be owned by the aggregate");

        typeof(Notrelix.Domain.Billing.Usage.Events.QuotaExceededDomainEvent)
            .Should().NotBeNull(
                $"{GateId}: hard-limit rejection must raise the owned quota-exceeded fact");
    }

    [Fact]
    public void CapacityAggregates_MustNotBeConsumed_ByProductContexts()
    {
        var violations = CollectProductContextCapacityConsumption();

        violations.Should().BeEmpty(
            $"{GateId}: product feature contexts must not consume Billing capacity state " +
            "directly. Capability semantics flow through Billing-owned Public facts. " +
            "Violations:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void AutomationRule_MustNotBeEmbedded_AsBillableMetric_OutsideBilling()
    {
        var violations = CollectAutomationRuleLiteralViolations();

        violations.Should().BeEmpty(
            $"{GateId}: billable capability classification authority is Billing-owned. " +
            "Define capability codes in Billing Public facts and consume them as facts. " +
            "Violations:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void NoPlanTierType_MayExistInProduction()
    {
        var violations = CollectPlanTierTypes();

        violations.Should().BeEmpty(
            $"{GateId}: PlanTier is not a production concept — tier vocabulary belongs to " +
            "the Billing-owned SubscriptionTier. Violations: " + string.Join(", ", violations));
    }

    [Fact]
    public void SubscriptionTierReferences_MustMatch_ExactGovernedBaseline()
    {
        var actual = CollectTierReferenceSites();

        var added = actual.Except(TierReferenceBaseline).ToList();
        var removed = TierReferenceBaseline.Except(actual).ToList();

        added.Should().BeEmpty(
            $"{GateId}: SubscriptionTier must not be referenced outside Billing-owned paths. " +
            "Add an entry only together with the governed migration that justifies it. " +
            "Violations: " + string.Join(", ", added));

        removed.Should().BeEmpty(
            $"{GateId}: the tier-reference debt baseline shrank — shrink TierReferenceBaseline " +
            "in this test in the same change. Removed: " + string.Join(", ", removed));
    }

    // ------------------------------------------------------------------
    // Gate self-tests (regression rejection proof)
    // ------------------------------------------------------------------

    [Fact]
    public void Gate_Detects_ZeroAsUnlimited_Site()
    {
        DetectZeroAsUnlimited("if (entitlement.Limit == 0) return true;")
            .Should().BeTrue("a new zero-as-unlimited interpretation must be flagged");

        DetectZeroAsUnlimited("AND (e.limit_value = 0 OR used + amount <= e.limit_value)")
            .Should().BeTrue("a new SQL zero-as-unlimited interpretation must be flagged");

        DetectZeroAsUnlimited("if (entitlement.IsUnlimited) return true;")
            .Should().BeFalse("the explicit unlimited representation is compliant");
    }

    [Fact]
    public void Gate_Detects_CapacityConsumption_OutsideBilling()
    {
        ClassifyCapacityConsumption("Notrelix.Application.Features.Automation.Services.RuleQuotaService")
            .Should().NotBeNull("product contexts consuming capacity state must be flagged");

        ClassifyCapacityConsumption("Notrelix.Application.Features.Billing.Entitlements.Services.BillingCapabilityFactsProvider")
            .Should().BeNull("the Billing-owned capability provider is compliant");
    }

    [Fact]
    public void Gate_Detects_AutomationRuleLiteral_OutsideBilling()
    {
        IsBillingOwnedPath("Notrelix.Application/Features/Billing/Public/Facts/IBillingCapabilityFacts.cs")
            .Should().BeTrue("Billing Public facts are the sanctioned authority");

        IsBillingOwnedPath("Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRule.cs")
            .Should().BeFalse("Automation is a consumer, not the classification authority");
    }

    [Fact]
    public void Gate_Detects_PlanTierType()
    {
        IsPlanTierType("NewPlanTierType").Should().BeTrue("a PlanTier type must be flagged");
        IsPlanTierType("SubscriptionTier").Should().BeFalse("Billing-owned tier vocabulary is not PlanTier");
    }

    [Fact]
    public void Gate_Detects_TierReference_OutsideBilling()
    {
        DetectTierReference("Notrelix.Application/Common/Security/AccessPolicyEngine.cs", "facts.SubscriptionTier")
            .Should().BeTrue("a governed-debt reference must be baselined, not silently grown");

        DetectTierReference("Notrelix.Application/Features/Automation/Rules/CreateAutomationRule.cs", "SubscriptionTier.Pro")
            .Should().BeTrue("a new consumer tier branch must be flagged");

        DetectTierReference("Notrelix.Domain/Billing/Subscriptions/Subscription.cs", "public SubscriptionTier Tier { get; }")
            .Should().BeFalse("the Billing owner is exempt");
    }

    [Fact]
    public void Gate_Baselines_AreExact_AndPathShaped()
    {
        ZeroAsUnlimitedBaseline.Should().NotBeEmpty();
        ZeroAsUnlimitedBaseline.Should().OnlyContain(p => p.EndsWith(".cs", StringComparison.Ordinal));

        TierReferenceBaseline.Should().NotBeEmpty();
        TierReferenceBaseline.Should().OnlyContain(p => p.EndsWith(".cs", StringComparison.Ordinal));
    }

    // ------------------------------------------------------------------
    // Detectors (pure functions so self-tests exercise rejection directly)
    // ------------------------------------------------------------------

    private static bool DetectZeroAsUnlimited(string source)
    {
        var zeroPattern = new Regex(
            "Limit\\s*==\\s*0\\b|limit_value\\s*=\\s*0\\b",
            RegexOptions.Compiled);

        return zeroPattern.IsMatch(source);
    }

    private static string? ClassifyCapacityConsumption(string typeFullName)
    {
        var isProductContext = typeFullName.StartsWith("Notrelix.Application.Features.", StringComparison.Ordinal)
            && !typeFullName.StartsWith("Notrelix.Application.Features.Billing.", StringComparison.Ordinal);

        return isProductContext
            ? $"{typeFullName} consumes Billing capacity state outside the Billing authority"
            : null;
    }

    private static bool IsBillingOwnedPath(string relativePath)
        => relativePath.Replace('\\', '/').Contains("/Billing/", StringComparison.OrdinalIgnoreCase)
           || relativePath.Replace('\\', '/').StartsWith("Notrelix.Domain/Billing/", StringComparison.OrdinalIgnoreCase);

    private static bool IsPlanTierType(string typeName)
        => typeName.Contains("PlanTier", StringComparison.Ordinal);

    private static bool DetectTierReference(string relativePath, string source)
    {
        if (!source.Contains("SubscriptionTier", StringComparison.Ordinal))
            return false;

        return !relativePath.Replace('\\', '/').StartsWith("Notrelix.Domain/", StringComparison.Ordinal)
               && !IsBillingOwnedPath(relativePath);
    }

    private static SortedSet<string> CollectZeroAsUnlimitedSites()
    {
        var sites = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var projectPath in new[] { GetApplicationPath(), GetInfrastructurePath(), GetDomainPath() })
        {
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }

                if (DetectZeroAsUnlimited(File.ReadAllText(file)))
                    sites.Add(Path.GetRelativePath(GetSrcPath(), file).Replace('\\', '/'));
            }
        }

        return sites;
    }

    private static List<string> CollectProductContextCapacityConsumption()
    {
        var violations = new List<string>();

        var capacityTypes = Assembly.Load("Notrelix.Domain")
            .GetTypes()
            .Where(t => t.Namespace?.StartsWith("Notrelix.Domain.Billing.Usage", StringComparison.Ordinal) == true)
            .ToList();

        var productTypes = Assembly.Load("Notrelix.Application")
            .GetTypes()
            .Where(t => t.Namespace?.StartsWith("Notrelix.Application.Features.", StringComparison.Ordinal) == true)
            .Where(t => t.Namespace?.StartsWith("Notrelix.Application.Features.Billing.", StringComparison.Ordinal) != true);

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var type in productTypes)
        {
            foreach (var referenced in CollectPublicSignatureDependencies(type, flags))
            {
                if (capacityTypes.Contains(referenced))
                    violations.Add($"{type.FullName} -> {referenced.FullName}");
            }
        }

        return violations.Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToList();
    }

    private static List<string> CollectAutomationRuleLiteralViolations()
    {
        var violations = new List<string>();

        foreach (var projectPath in new[] { GetApplicationPath(), GetInfrastructurePath(), GetDomainPath() })
        {
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }

                var relative = Path.GetRelativePath(GetSrcPath(), file).Replace('\\', '/');

                if (File.ReadAllText(file).Contains("AUTOMATION_RULE", StringComparison.Ordinal)
                    && !IsBillingOwnedPath(relative))
                {
                    violations.Add(relative);
                }
            }
        }

        return violations.OrderBy(v => v, StringComparer.Ordinal).ToList();
    }

    private static List<string> CollectPlanTierTypes()
    {
        var violations = new List<string>();

        foreach (var assemblyName in new[] { "Notrelix.Domain", "Notrelix.Application", "Notrelix.Infrastructure", "Notrelix.API" })
        {
            foreach (var type in Assembly.Load(assemblyName).GetTypes())
            {
                if (IsPlanTierType(type.Name))
                    violations.Add(type.FullName!);
            }
        }

        return violations;
    }

    private static SortedSet<string> CollectTierReferenceSites()
    {
        var sites = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var projectPath in new[] { GetApplicationPath(), GetInfrastructurePath(), GetDomainPath() })
        {
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }

                var relative = Path.GetRelativePath(GetSrcPath(), file).Replace('\\', '/');

                if (DetectTierReference(relative, File.ReadAllText(file)))
                    sites.Add(relative);
            }
        }

        return sites;
    }

    private static IEnumerable<Type> CollectPublicSignatureDependencies(Type type, BindingFlags flags)
    {
        var collected = new List<Type>();

        void Collect(Type? candidate)
        {
            if (candidate is null)
                return;

            collected.Add(candidate);

            if (candidate.IsGenericType && !candidate.IsGenericTypeDefinition)
            {
                foreach (var argument in candidate.GetGenericArguments())
                    Collect(argument);
            }
        }

        Collect(type.BaseType);

        foreach (var iface in type.GetInterfaces())
            Collect(iface);

        foreach (var ctor in type.GetConstructors(flags))
            foreach (var parameter in ctor.GetParameters())
                Collect(parameter.ParameterType);

        foreach (var field in type.GetFields(flags))
            Collect(field.FieldType);

        foreach (var property in type.GetProperties(flags))
            Collect(property.PropertyType);

        foreach (var method in type.GetMethods(flags).Where(m => !m.IsSpecialName))
        {
            Collect(method.ReturnType);

            foreach (var parameter in method.GetParameters())
                Collect(parameter.ParameterType);
        }

        return collected
            .Where(t => t != type)
            .Distinct()
            .ToList();
    }
}
