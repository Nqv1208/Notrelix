using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Discovers every public instance mutation on every Frozen AggregateRoot,
/// resolves exact signatures, and validates [CoversMutation] coverage.
/// </summary>
public class MutationCoverageTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;
    private static readonly Assembly TestsAssembly = typeof(MutationCoverageTests).Assembly;

    private static readonly HashSet<string> SkipMethodPrefixes =
    [
        "get_", "set_", "add_", "remove_", // property/event accessors
        "Equals", "GetHashCode", "ToString", "GetType", // object methods
        "Clone", // record clone
        "<Clone>$", // record clone compiler-generated
        "Deconstruct", // record deconstruct
    ];

    private static List<Type> GetFrozenAggregateRoots()
    {
        return DomainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                     && typeof(AggregateRoot).IsAssignableFrom(t)
                     && t.Namespace?.StartsWith("Notrelix.Domain.", StringComparison.Ordinal) == true)
            .Where(t => DomainCapabilityRegistry.ResolveCapability(t) == DomainCapabilityStatus.Frozen)
            .OrderBy(t => t.FullName)
            .ToList();
    }

    private static List<MethodInfo> GetMutationMethods(Type aggregateType)
    {
        return aggregateType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Where(m => !SkipMethodPrefixes.Any(p => m.Name.StartsWith(p, StringComparison.Ordinal)))
            .ToList();
    }

    private static List<(Type TestType, MethodInfo TestMethod, CoversMutationAttribute Attribute)> GetCoveredMutations()
    {
        var results = new List<(Type, MethodInfo, CoversMutationAttribute)>();

        foreach (var testType in TestsAssembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
        {
            foreach (var method in testType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var attrs = method.GetCustomAttributes<CoversMutationAttribute>(inherit: false);
                foreach (var attr in attrs)
                {
                    results.Add((testType, method, attr));
                }
            }
        }

        return results;
    }

    [Fact]
    public void FrozenAggregates_MutationMethods_ShouldExistExactlyOnce()
    {
        var violations = new List<string>();

        foreach (var aggregate in GetFrozenAggregateRoots())
        {
            var methods = GetMutationMethods(aggregate);

            foreach (var method in methods)
            {
                var signature = MutationSignatureFormatter.Format(method);

                // Count occurrences of this exact signature on the aggregate
                var occurrences = methods.Count(m =>
                    MutationSignatureFormatter.Format(m) == signature);

                if (occurrences != 1)
                {
                    violations.Add($"{aggregate.FullName}.{signature} exists {occurrences} times");
                }
            }
        }

        violations.Should().BeEmpty(
            "every mutation method must exist exactly once: " +
            string.Join("\n", violations));
    }

    [Fact(Skip = "Pending [CoversMutation] attribute rollout — informational gate")]
    public void FrozenAggregates_ShouldHaveCoversMutationForEveryMutation()
    {
        var covered = GetCoveredMutations();

        // Build lookup: aggregate type -> list of covered signatures
        var coverageByAggregate = new Dictionary<Type, HashSet<string>>();
        foreach (var (_, _, attr) in covered)
        {
            if (!coverageByAggregate.TryGetValue(attr.AggregateType, out var sigs))
            {
                sigs = new HashSet<string>();
                coverageByAggregate[attr.AggregateType] = sigs;
            }
            sigs.Add(attr.MethodSignature);
        }

        var violations = new List<string>();

        foreach (var aggregate in GetFrozenAggregateRoots())
        {
            var methods = GetMutationMethods(aggregate);

            foreach (var method in methods)
            {
                var signature = MutationSignatureFormatter.Format(method);

                if (!coverageByAggregate.TryGetValue(aggregate, out var sigs) || !sigs.Contains(signature))
                {
                    violations.Add($"{aggregate.FullName}.{signature} lacks [CoversMutation]");
                }
            }
        }

        violations.Should().BeEmpty(
            "every mutation on a frozen aggregate must have [CoversMutation] coverage. " +
            $"Missing: {string.Join("\n", violations)}");
    }

    [Fact]
    public void CoversMutation_Signatures_ShouldExistOnTargetType()
    {
        var covered = GetCoveredMutations();
        var violations = new List<string>();

        foreach (var (testType, testMethod, attr) in covered)
        {
            var methods = attr.AggregateType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => MutationSignatureFormatter.Format(m) == attr.MethodSignature)
                .ToList();

            if (methods.Count == 0)
            {
                violations.Add(
                    $"{testType.Name}.{testMethod.Name} references non-existent " +
                    $"{attr.AggregateType.Name}.{attr.MethodSignature}");
            }
        }

        violations.Should().BeEmpty(
            "[CoversMutation] signatures must reference methods that exist on the target type: " +
            string.Join("\n", violations));
    }

    [Fact]
    public void CoversMutation_TestMethods_ShouldBeValidTestMethods()
    {
        var covered = GetCoveredMutations();
        var violations = new List<string>();

        foreach (var (testType, testMethod, _) in covered)
        {
            var hasFact = testMethod.GetCustomAttributes(typeof(FactAttribute), true).Length > 0;
            var hasTheory = testMethod.GetCustomAttributes(typeof(TheoryAttribute), true).Length > 0;

            if (!hasFact && !hasTheory)
            {
                violations.Add(
                    $"{testType.Name}.{testMethod.Name} is marked [CoversMutation] " +
                    "but is not a [Fact] or [Theory] method");
            }
        }

        violations.Should().BeEmpty(
            "[CoversMutation] must be on [Fact] or [Theory] methods: " +
            string.Join("\n", violations));
    }
}
