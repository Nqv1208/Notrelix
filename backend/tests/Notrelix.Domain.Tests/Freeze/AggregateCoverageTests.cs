using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Discovers all concrete AggregateRoot subclasses in the Domain assembly
/// and asserts mutation coverage via [CoversAggregate] attributes on test fixtures.
/// </summary>
public class AggregateCoverageTests
{
    private static Assembly DomainAssembly => typeof(AggregateRoot).Assembly;
    private static Assembly TestsAssembly => typeof(AggregateCoverageTests).Assembly;

    private static List<Type> GetAggregateRoots()
    {
        return DomainAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                     && typeof(AggregateRoot).IsAssignableFrom(t))
            .OrderBy(t => t.FullName)
            .ToList();
    }

    private static Dictionary<Type, List<Type>> GetCoverageMap()
    {
        var map = new Dictionary<Type, List<Type>>();

        var testTypes = TestsAssembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract);

        foreach (var testType in testTypes)
        {
            var attrs = testType.GetCustomAttributes<CoversAggregateAttribute>(inherit: false);
            foreach (var attr in attrs)
            {
                if (!map.TryGetValue(attr.AggregateType, out var list))
                {
                    list = new List<Type>();
                    map[attr.AggregateType] = list;
                }
                list.Add(testType);
            }
        }

        return map;
    }

    [Fact]
    public void DomainAssembly_ShouldContain_SubstantialAggregateRoots()
    {
        var aggregateRoots = GetAggregateRoots();
        var names = aggregateRoots.Select(t => t.FullName).ToList();

        aggregateRoots.Should().HaveCountGreaterThan(59,
            $"expected at least 60 concrete AggregateRoot subclasses but found {aggregateRoots.Count}: " +
            string.Join(", ", names));
    }

    [Fact]
    public void AllAggregateRoots_ShouldHaveParameterlessOrGuidIdConstructor()
    {
        var aggregateRoots = GetAggregateRoots();

        foreach (var type in aggregateRoots)
        {
            var constructors = type.GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            constructors.Should().NotBeEmpty(
                $"{type.FullName} should have at least one constructor");
        }
    }

    [Fact]
    public void AllAggregateRoots_ShouldBeInExpectedNamespaces()
    {
        var aggregateRoots = GetAggregateRoots();

        var unexpectedNamespaces = aggregateRoots
            .Where(t => !t.Namespace!.StartsWith("Notrelix.Domain."))
            .Select(t => t.FullName)
            .ToList();

        unexpectedNamespaces.Should().BeEmpty(
            "all AggregateRoot subclasses should be in the Notrelix.Domain.* namespace hierarchy");
    }

    [Fact]
    public void AllFrozenAggregates_ShouldHaveCoversAggregateAttribute()
    {
        var aggregateRoots = GetAggregateRoots();
        var coverageMap = GetCoverageMap();

        var uncovered = aggregateRoots
            .Where(t => !coverageMap.ContainsKey(t))
            .Select(t => t.FullName)
            .ToList();

        uncovered.Should().BeEmpty(
            "every frozen aggregate must have a [CoversAggregate] fixture. " +
            $"Missing: {string.Join(", ", uncovered)}");
    }

    [Fact]
    public void AllAggregateRoots_ShouldHaveMutationTestCoverage()
    {
        var aggregateRoots = GetAggregateRoots();
        var coverageMap = GetCoverageMap();

        var missingMutationCoverage = new List<string>();

        foreach (var aggregate in aggregateRoots)
        {
            if (!coverageMap.TryGetValue(aggregate, out var fixtures) || fixtures.Count == 0)
            {
                missingMutationCoverage.Add(aggregate.FullName!);
                continue;
            }

            // Check that at least one fixture has executable test methods
            var hasExecutableTests = fixtures.Any(f =>
                f.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Any(m => m.GetCustomAttributes(typeof(FactAttribute), true).Length > 0 ||
                              m.GetCustomAttributes(typeof(TheoryAttribute), true).Length > 0));

            if (!hasExecutableTests)
            {
                missingMutationCoverage.Add($"{aggregate.FullName} (fixtures have no [Fact] or [Theory] methods)");
            }
        }

        missingMutationCoverage.Should().BeEmpty(
            "every aggregate must have executable test methods in its fixtures. " +
            $"Missing: {string.Join("; ", missingMutationCoverage)}");
    }

    [Fact]
    public void CoversAggregateAttribute_ShouldOnlyReferenceValidAggregates()
    {
        var aggregateRoots = GetAggregateRoots().ToHashSet();
        var coverageMap = GetCoverageMap();

        var invalidReferences = coverageMap
            .Where(kvp => !aggregateRoots.Contains(kvp.Key))
            .Select(kvp => kvp.Key.FullName)
            .ToList();

        invalidReferences.Should().BeEmpty(
            $"[CoversAggregate] attributes must reference concrete AggregateRoot types; invalid: {string.Join(", ", invalidReferences)}");
    }
}
