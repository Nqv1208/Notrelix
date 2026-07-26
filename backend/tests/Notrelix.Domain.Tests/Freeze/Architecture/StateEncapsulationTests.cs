using System.Collections.ObjectModel;
using System.Reflection;
using FluentAssertions;
using Notrelix.Domain.Common;
using Xunit;

namespace Notrelix.Domain.Tests.Freeze.Architecture;

public class StateEncapsulationTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    [Fact]
    public void AggregateEntities_ShouldNotExposeMutableCollections()
    {
        var mutableCollectionTypes = new[]
        {
            typeof(List<>),
            typeof(HashSet<>),
            typeof(Dictionary<,>),
            typeof(Collection<>),
            typeof(ObservableCollection<>),
        };

        var violations = new List<string>();

        foreach (var type in DomainAssembly.GetTypes())
        {
            if (!IsDomainType(type)) continue;
            if (type.IsEnum || type.IsInterface || type.IsAbstract) continue;

            if (typeof(AggregateRoot).IsAssignableFrom(type) ||
                typeof(Entity).IsAssignableFrom(type))
            {
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var propType = prop.PropertyType;

                    // Check direct mutable collection types
                    if (propType.IsGenericType && mutableCollectionTypes.Contains(propType.GetGenericTypeDefinition()))
                    {
                        violations.Add($"{type.FullName}.{prop.Name} (type: {propType.Name})");
                    }

                    // Check IEnumerable<T> properties that return concrete mutable types
                    if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        violations.Add($"{type.FullName}.{prop.Name} (exposes IEnumerable<T> - prefer IReadOnlyList<T>)");
                    }
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
