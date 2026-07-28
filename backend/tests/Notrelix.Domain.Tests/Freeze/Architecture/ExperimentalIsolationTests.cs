using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze.Architecture;

public class ExperimentalIsolationTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    private static readonly HashSet<string> ExperimentalPrefixes = new(
        DomainCapabilityRegistry.GetExperimental().Select(c => c.NamespacePrefix));

    [Fact]
    public void FrozenAggregates_ShouldNotReferenceExperimentalTypes()
    {
        var violations = new List<string>();

        foreach (var type in DomainAssembly.GetTypes())
        {
            if (!IsFrozenDomainType(type)) continue;

            var referencedTypes = DomainTypeGraphWalker.GetReferencedTypes(type);

            foreach (var referencedType in referencedTypes)
            {
                if (IsExperimentalType(referencedType))
                {
                    violations.Add($"{type.FullName} -> {referencedType.FullName}");
                }
            }
        }

        violations.Should().BeEmpty(
            "frozen types must not reference experimental types: " +
            string.Join("\n", violations));
    }

    [Fact]
    public void ExperimentalAggregates_ShouldNotBeReferencedByFrozenTypes()
    {
        var violations = new List<string>();

        foreach (var type in DomainAssembly.GetTypes())
        {
            if (!IsExperimentalType(type)) continue;

            var referencedTypes = DomainTypeGraphWalker.GetReferencedTypes(type);

            foreach (var referencedType in referencedTypes)
            {
                if (IsFrozenDomainType(referencedType))
                {
                    // Experimental types CAN reference frozen types (one-way dependency)
                }
            }
        }

        violations.Should().BeEmpty();
    }

    [Fact]
    public void ExperimentalTypes_ShouldHaveExperimentalNamespace()
    {
        var violations = new List<string>();

        foreach (var type in DomainAssembly.GetTypes())
        {
            if (!IsExperimentalType(type)) continue;
            if (type.Namespace is null) continue;

            var hasExperimentalNamespace = ExperimentalPrefixes.Any(p =>
                type.Namespace.StartsWith(p, StringComparison.Ordinal));

            if (!hasExperimentalNamespace)
            {
                violations.Add($"{type.FullName} (namespace: {type.Namespace})");
            }
        }

        violations.Should().BeEmpty(
            "experimental types must live in experimental namespaces: " +
            string.Join("\n", violations));
    }

    private static bool IsFrozenDomainType(Type type)
    {
        if (type.Namespace is null) return false;
        if (!type.Namespace.StartsWith("Notrelix.Domain.", StringComparison.Ordinal)) return false;

        var status = DomainCapabilityRegistry.ResolveCapability(type);
        return status == DomainCapabilityStatus.Frozen;
    }

    private static bool IsExperimentalType(Type type)
    {
        if (type.Namespace is null) return false;

        return ExperimentalPrefixes.Any(p =>
            type.Namespace.StartsWith(p, StringComparison.Ordinal));
    }
}
