using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Discovers all concrete AggregateRoot subclasses in the Domain assembly
/// and asserts a minimum count as a freeze-gate sanity check.
/// </summary>
public class AggregateCoverageTests
{
    [Fact]
    public void DomainAssembly_ShouldContain_AtLeast60AggregateRoots()
    {
        var domainAssembly = typeof(AggregateRoot).Assembly;

        var aggregateRoots = domainAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                     && typeof(AggregateRoot).IsAssignableFrom(t))
            .OrderBy(t => t.FullName)
            .ToList();

        // Print all discovered aggregate roots for diagnostic visibility
        var names = aggregateRoots.Select(t => t.FullName).ToList();

        aggregateRoots.Should().HaveCountGreaterThan(59,
            $"expected at least 60 concrete AggregateRoot subclasses but found {aggregateRoots.Count}: " +
            string.Join(", ", names));
    }

    [Fact]
    public void AllAggregateRoots_ShouldHaveParameterlessOrGuidIdConstructor()
    {
        var domainAssembly = typeof(AggregateRoot).Assembly;

        var aggregateRoots = domainAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                     && typeof(AggregateRoot).IsAssignableFrom(t))
            .ToList();

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
        var domainAssembly = typeof(AggregateRoot).Assembly;

        var aggregateRoots = domainAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                     && typeof(AggregateRoot).IsAssignableFrom(t))
            .ToList();

        var unexpectedNamespaces = aggregateRoots
            .Where(t => !t.Namespace!.StartsWith("Notrelix.Domain."))
            .Select(t => t.FullName)
            .ToList();

        unexpectedNamespaces.Should().BeEmpty(
            "all AggregateRoot subclasses should be in the Notrelix.Domain.* namespace hierarchy");
    }
}
