using System.Reflection;
using FluentAssertions;
using Notrelix.Domain.Common;

namespace Notrelix.Architecture.Tests;

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

    private static readonly string[] ExperimentalNamespacePrefixes =
    [
        // WorkManagement experimental
        "Notrelix.Domain.WorkManagement.Approvals",
        "Notrelix.Domain.WorkManagement.Formulas",
        "Notrelix.Domain.WorkManagement.Rollups",
        "Notrelix.Domain.WorkManagement.Workload",
        // Collaboration experimental
        "Notrelix.Domain.Collaboration.Presence",
        // Automation experimental (runtime/orchestration)
        "Notrelix.Domain.Automation.Triggers",
        "Notrelix.Domain.Automation.Actions",
        "Notrelix.Domain.Automation.Conditions",
        "Notrelix.Domain.Automation.Executions",
        "Notrelix.Domain.Automation.Agents",
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

            var referencedTypes = DomainTypeGraphWalker.GetReferencedTypes(type);

            foreach (var referencedType in referencedTypes)
            {
                // Skip primitive types, value objects, enums, Guid, string, etc.
                if (IsPrimitiveOrBuiltin(referencedType)) continue;

                // Check if the target type is a concrete entity from another context
                if (referencedType is { IsClass: true, IsAbstract: false } &&
                    typeof(AggregateRoot).IsAssignableFrom(referencedType))
                {
                    var targetContext = ResolveContext(referencedType.Namespace!);

                    if (targetContext is not null && targetContext != sourceContext)
                    {
                        violations.Add($"{type.FullName} -> {referencedType.FullName} (cross-context)");
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "aggregates must not reference concrete entities from other contexts (use IDs instead): " +
            string.Join("\n", violations));
    }

    [Fact]
    public void FrozenTypes_ShouldNotReferenceEntitiesFromOtherContexts()
    {
        var violations = new List<string>();

        foreach (var type in DomainAssembly.GetTypes())
        {
            if (!IsFrozenDomainType(type)) continue;

            var referencedTypes = DomainTypeGraphWalker.GetReferencedTypes(type);

            foreach (var referencedType in referencedTypes)
            {
                if (IsPrimitiveOrBuiltin(referencedType)) continue;

                if (IsEntityOrAggregate(referencedType))
                {
                    var targetContext = ResolveContext(referencedType.Namespace!);

                    if (targetContext is not null && targetContext != ResolveContext(type.Namespace!))
                    {
                        violations.Add($"{type.FullName} -> {referencedType.FullName} (cross-context entity)");
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "frozen types must not reference concrete entities from other contexts (use IDs instead): " +
            string.Join("\n", violations));
    }

    private static bool IsFrozenDomainType(Type type)
    {
        if (type.Namespace is null) return false;
        if (!type.Namespace.StartsWith("Notrelix.Domain.", StringComparison.Ordinal)) return false;

        return !ExperimentalNamespacePrefixes.Any(p =>
            type.Namespace.StartsWith(p, StringComparison.Ordinal));
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

    private static bool IsEntityOrAggregate(Type type)
    {
        return typeof(Entity).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false };
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
        if (type.IsValueType) return true;
        if (type.IsArray) return true;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) return true;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)) return true;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)) return true;
        if (type.IsInterface) return true;
        return false;
    }
}
