using System.Collections.ObjectModel;
using System.Reflection;
using FluentAssertions;
using Notrelix.Domain.Common;

namespace Notrelix.Architecture.Tests;

public class DomainStateEncapsulationTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    private static readonly Type[] MutableCollectionTypes =
    [
        typeof(List<>),
        typeof(HashSet<>),
        typeof(Dictionary<,>),
        typeof(Collection<>),
        typeof(ObservableCollection<>),
    ];

    [Fact]
    public void AggregateEntities_ShouldNotExposeMutableCollections()
    {
        var violations = new List<string>();

        foreach (var type in DomainAssembly.GetTypes())
        {
            if (!IsDomainType(type)) continue;
            if (type.IsEnum || type.IsInterface || type.IsAbstract) continue;

            if (!typeof(AggregateRoot).IsAssignableFrom(type) &&
                !typeof(Entity).IsAssignableFrom(type))
                continue;

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var propType = prop.PropertyType;

                if (propType.IsGenericType && MutableCollectionTypes.Contains(propType.GetGenericTypeDefinition()))
                {
                    violations.Add($"{type.FullName}.{prop.Name} (type: {propType.Name})");
                }

                if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    violations.Add($"{type.FullName}.{prop.Name} (exposes IEnumerable<T> - prefer IReadOnlyList<T>)");
                }
            }
        }

        violations.Should().BeEmpty(
            "aggregate entities must not expose mutable collections (use IReadOnlyList<T> instead): " +
            string.Join("\n", violations));
    }

    private static bool IsDomainType(Type type)
    {
        if (type.Namespace is null) return false;
        return type.Namespace.StartsWith("Notrelix.Domain.", StringComparison.Ordinal);
    }
}
