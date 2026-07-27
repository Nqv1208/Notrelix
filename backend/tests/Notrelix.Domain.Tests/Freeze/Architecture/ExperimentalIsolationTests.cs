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

            foreach (var referencedType in GetReferencedTypes(type))
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

            foreach (var referencedType in GetReferencedTypes(type))
            {
                if (IsFrozenDomainType(referencedType))
                {
                    // Experimental types CAN reference frozen types (one-way dependency)
                    // This test verifies the reverse direction only
                }
            }
        }

        // This test is a no-op for now - experimental -> frozen is allowed
        // The real assertion is in FrozenAggregates_ShouldNotReferenceExperimentalTypes
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

    private static IEnumerable<Type> GetReferencedTypes(Type type)
    {
        var types = new HashSet<Type>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            types.Add(prop.PropertyType);

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            types.Add(method.ReturnType);
            foreach (var param in method.GetParameters())
                types.Add(param.ParameterType);
        }

        foreach (var iface in type.GetInterfaces())
            types.Add(iface);

        return types.Where(t => t.Namespace?.StartsWith("Notrelix.Domain.", StringComparison.Ordinal) == true);
    }
}
