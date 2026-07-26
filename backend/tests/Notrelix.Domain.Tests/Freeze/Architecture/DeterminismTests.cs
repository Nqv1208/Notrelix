using System.Globalization;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Notrelix.Domain.Tests.Freeze.Architecture;

public class DeterminismTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    private static readonly Type[] ForbiddenTypes =
    [
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(CultureInfo),
        typeof(Random),
    ];

    private static readonly HashSet<string> DeterminismWhitelist = new()
    {
        // Domain value objects that legitimately use these types
        "Notrelix.Domain.Common.FractionalIndex",
        "Notrelix.Domain.Common.ValueObjects.FractionalIndex",
    };

    [Fact]
    public void Domain_ShouldNotUseDateTimeUtcNow()
    {
        var violations = new List<string>();

        foreach (var type in DomainAssembly.GetTypes())
        {
            if (!IsDomainType(type)) continue;

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (field.FieldType == typeof(DateTime) || field.FieldType == typeof(DateTimeOffset))
                {
                    if (field.Name.Contains("UtcNow", StringComparison.OrdinalIgnoreCase))
                    {
                        violations.Add($"{type.FullName}.{field.Name}");
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "domain must not use DateTime.UtcNow (use IClock abstraction instead): " +
            string.Join(", ", violations));
    }

    [Fact]
    public void Domain_ShouldNotUseEnvironmentGetters()
    {
        var violations = new List<string>();

        foreach (var type in DomainAssembly.GetTypes())
        {
            if (!IsDomainType(type)) continue;

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (prop.DeclaringType == typeof(Environment))
                {
                    violations.Add($"{type.FullName} -> Environment.{prop.Name}");
                }
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.DeclaringType == typeof(Environment) && method.GetParameters().Length == 0)
                {
                    violations.Add($"{type.FullName} -> Environment.{method.Name}()");
                }
            }
        }

        violations.Should().BeEmpty(
            "domain must not use Environment static methods (use abstractions instead): " +
            string.Join(", ", violations));
    }

    [Fact]
    public void Domain_ShouldNotUseRandomShared()
    {
        var violations = new List<string>();

        foreach (var type in DomainAssembly.GetTypes())
        {
            if (!IsDomainType(type)) continue;

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (prop.PropertyType == typeof(Random) && prop.Name == "Shared")
                {
                    violations.Add($"{type.FullName}.Random.Shared");
                }
            }
        }

        violations.Should().BeEmpty(
            "domain must not use Random.Shared (use explicit Random instance or abstraction): " +
            string.Join(", ", violations));
    }

    [Fact]
    public void Domain_ShouldNotUseCurrentCulture()
    {
        var violations = new List<string>();

        foreach (var type in DomainAssembly.GetTypes())
        {
            if (!IsDomainType(type)) continue;

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (prop.DeclaringType == typeof(CultureInfo) && prop.Name == "CurrentCulture")
                {
                    violations.Add($"{type.FullName}.CultureInfo.CurrentCulture");
                }

                if (prop.DeclaringType == typeof(CultureInfo) && prop.Name == "CurrentUICulture")
                {
                    violations.Add($"{type.FullName}.CultureInfo.CurrentUICulture");
                }
            }
        }

        violations.Should().BeEmpty(
            "domain must not use CultureInfo.CurrentCulture (use invariant culture or abstraction): " +
            string.Join(", ", violations));
    }

    private static bool IsDomainType(Type type)
    {
        if (type.Namespace is null) return false;
        if (!type.Namespace.StartsWith("Notrelix.Domain.", StringComparison.Ordinal)) return false;
        if (type.Namespace.StartsWith("Notrelix.Domain.SharedKernel", StringComparison.Ordinal)) return false;

        // Skip whitelisted types
        if (DeterminismWhitelist.Contains(type.FullName!)) return false;

        return true;
    }
}
