using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze.Architecture;

public class CrossContextReferenceTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    private static readonly string[] ContextPrefixes =
    [
        "Notrelix.Domain.Accounts",
        "Notrelix.Domain.Identity",
        "Notrelix.Domain.Workspaces",
        "Notrelix.Domain.Teams",
        "Notrelix.Domain.Governance",
        "Notrelix.Domain.WorkManagement",
        "Notrelix.Domain.Documents",
        "Notrelix.Domain.Collaboration",
        "Notrelix.Domain.Automation",
        "Notrelix.Domain.Integrations",
        "Notrelix.Domain.Billing",
        "Notrelix.Domain.Analytics",
    ];

    private static readonly HashSet<string> CrossContextWhitelist = new()
    {
        // Governance → Workspaces (PermissionTemplate references WorkspaceId)
        "Notrelix.Domain.Governance.Templates.PermissionTemplate.WorkspaceId",
    };

    [Fact]
    public void Aggregates_ShouldNotReferenceConcreteEntitiesFromOtherContexts()
    {
        var violations = new List<string>();

        foreach (var type in DomainAssembly.GetTypes())
        {
            if (!IsDomainType(type)) continue;
            if (!IsAggregateRoot(type)) continue;

            var sourceContext = ResolveContext(type.Namespace!);

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var targetType = prop.PropertyType;

                // Skip primitive types, value objects, enums, Guid, string, etc.
                if (IsPrimitiveOrBuiltin(targetType)) continue;

                // Check if the target type is a concrete entity from another context
                if (targetType is { IsClass: true, IsAbstract: false } &&
                    typeof(AggregateRoot).IsAssignableFrom(targetType))
                {
                    var targetContext = ResolveContext(targetType.Namespace!);

                    if (targetContext is not null && targetContext != sourceContext)
                    {
                        var key = $"{type.FullName}.{prop.Name}";
                        if (!CrossContextWhitelist.Contains(key))
                        {
                            violations.Add($"{type.FullName}.{prop.Name} -> {targetType.FullName} (cross-context)");
                        }
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "aggregates must not reference concrete entities from other contexts (use IDs instead): " +
            string.Join("\n", violations));
    }

    private static bool IsDomainType(Type type)
    {
        if (type.Namespace is null) return false;
        return type.Namespace.StartsWith("Notrelix.Domain.", StringComparison.Ordinal);
    }

    private static bool IsAggregateRoot(Type type)
    {
        return typeof(AggregateRoot).IsAssignableFrom(type);
    }

    private static string? ResolveContext(string ns)
    {
        foreach (var prefix in ContextPrefixes)
        {
            if (ns.StartsWith(prefix, StringComparison.Ordinal))
                return prefix;
        }
        return null;
    }

    private static bool IsPrimitiveOrBuiltin(Type type)
    {
        if (type.IsPrimitive) return true;
        if (type == typeof(string)) return true;
        if (type == typeof(Guid)) return true;
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return true;
        if (type == typeof(decimal)) return true;
        if (type == typeof(byte[])) return true;
        if (type.IsEnum) return true;
        if (type.IsValueType) return true; // Value objects
        if (type.IsArray) return true;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) return true;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)) return true;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)) return true;
        if (type.IsInterface) return true; // Skip interfaces (IWorkspaceScoped, etc.)
        return false;
    }
}
