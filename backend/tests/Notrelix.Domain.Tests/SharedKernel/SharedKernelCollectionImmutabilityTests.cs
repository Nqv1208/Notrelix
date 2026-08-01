using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.SharedKernel;

/// <summary>
/// Verifies that SharedKernel value objects are structurally immutable.
/// No stored collection should be directly assignable from caller-provided references.
/// </summary>
public class SharedKernelCollectionImmutabilityTests
{
    [Fact]
    public void SharedKernel_HasNoStoredCollectionProperties()
    {
        var sharedKernelTypes = typeof(SharedKernelRuleCodes).Assembly
            .GetTypes()
            .Where(t => t.Namespace?.StartsWith("Notrelix.Domain.SharedKernel") == true
                        && !t.IsAbstract
                        && !t.IsInterface
                        && !t.IsEnum)
            .ToList();

        var violations = new List<string>();

        foreach (var type in sharedKernelTypes)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                var propType = prop.PropertyType;

                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propType)
                    && propType != typeof(string))
                {
                    violations.Add($"{type.Name}.{prop.Name} ({propType.Name})");
                }

                if (propType.IsGenericType)
                {
                    var genericDef = propType.GetGenericTypeDefinition();
                    if (genericDef == typeof(IReadOnlyList<>) ||
                        genericDef == typeof(IReadOnlyCollection<>) ||
                        genericDef == typeof(IReadOnlyDictionary<,>))
                    {
                        violations.Add($"{type.Name}.{prop.Name} ({propType.Name})");
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "SharedKernel value objects must not store collection properties directly: " +
            string.Join(", ", violations));
    }

    [Fact]
    public void FractionalIndexGenerator_ReturnTypeIsSealed()
    {
        typeof(FractionalIndexGenerator).IsAbstract.Should().BeTrue();
        typeof(FractionalIndexGenerator).IsSealed.Should().BeTrue();
    }

    [Fact]
    public void FractionalIndex_IsSealedAndImmutable()
    {
        var type = typeof(FractionalIndex);
        type.IsSealed.Should().BeTrue();

        var valueProp = type.GetProperty("Value");
        valueProp.Should().NotBeNull();
        valueProp!.GetSetMethod(true).Should().BeNull("Value should have no public setter");
    }
}
