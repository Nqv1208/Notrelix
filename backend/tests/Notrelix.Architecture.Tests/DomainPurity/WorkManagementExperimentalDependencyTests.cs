using System.Reflection;
using FluentAssertions;
using Notrelix.Domain.Common;

namespace Notrelix.Architecture.Tests;

public class WorkManagementExperimentalDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    private static readonly string[] ExperimentalNamespacePrefixes =
    [
        "Notrelix.Domain.WorkManagement.Approvals",
        "Notrelix.Domain.WorkManagement.Formulas",
        "Notrelix.Domain.WorkManagement.Rollups",
        "Notrelix.Domain.WorkManagement.Workload",
    ];

    private static readonly Type[] ExperimentalTypes =
    [
        ..DomainAssembly.GetTypes()
            .Where(t => t.Namespace is not null &&
                ExperimentalNamespacePrefixes.Any(p => t.Namespace.StartsWith(p, StringComparison.Ordinal)))
    ];

    private static readonly Type[] CoreTypes =
    [
        ..DomainAssembly.GetTypes()
            .Where(t => t.Namespace is not null &&
                t.Namespace.StartsWith("Notrelix.Domain.WorkManagement.", StringComparison.Ordinal) &&
                !ExperimentalNamespacePrefixes.Any(p => t.Namespace.StartsWith(p, StringComparison.Ordinal)) &&
                !t.Namespace.EndsWith("WorkManagementRuleCodes", StringComparison.Ordinal))
    ];

    [Fact]
    public void CoreTypes_ShouldNotReferenceExperimentalTypes()
    {
        var experimentalTypeNames = ExperimentalTypes.Select(t => t.FullName!).ToHashSet();
        var violations = new List<string>();

        foreach (var coreType in CoreTypes)
        {
            if (coreType.IsGenericTypeDefinition)
            {
                foreach (var arg in coreType.GetGenericArguments())
                {
                    if (experimentalTypeNames.Contains(arg.FullName!))
                        violations.Add($"{coreType.FullName} generic arg references {arg.FullName}");
                }
            }

            foreach (var property in coreType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var propType = property.PropertyType;
                if (experimentalTypeNames.Contains(propType.FullName!))
                    violations.Add($"{coreType.FullName}.{property.Name} returns {propType.FullName}");
            }

            foreach (var method in coreType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.IsSpecialName) continue;
                foreach (var param in method.GetParameters())
                {
                    if (experimentalTypeNames.Contains(param.ParameterType.FullName!))
                        violations.Add($"{coreType.FullName}.{method.Name} param {param.Name} is {param.ParameterType.FullName}");
                }
                if (experimentalTypeNames.Contains(method.ReturnType.FullName!))
                    violations.Add($"{coreType.FullName}.{method.Name} returns {method.ReturnType.FullName}");
            }

            foreach (var iface in coreType.GetInterfaces())
            {
                if (experimentalTypeNames.Contains(iface.FullName!))
                    violations.Add($"{coreType.FullName} implements {iface.FullName}");
            }

            var referencedTypes = DomainTypeGraphWalker.GetReferencedTypes(coreType);
            foreach (var referencedType in referencedTypes)
            {
                if (experimentalTypeNames.Contains(referencedType.FullName!))
                    violations.Add($"{coreType.FullName} -> {referencedType.FullName}");
            }
        }

        violations.Should().BeEmpty(
            "core WorkManagement types must not reference experimental WorkManagement types. " +
            $"Found {violations.Count} violations.");
    }

    [Fact]
    public void ExperimentalTypes_ShouldBePublic()
    {
        foreach (var type in ExperimentalTypes)
        {
            if (type.IsClass && !type.IsAbstract && !type.IsInterface && !type.IsEnum && !type.IsNested && !IsCompilerGenerated(type))
            {
                type.IsPublic.Should().BeTrue($"{type.FullName} should be public");
            }
        }
    }

    private static bool IsCompilerGenerated(Type type)
    {
        return type.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false).Length > 0 ||
               type.Name.Contains('<') ||
               type.Name.Contains("d__");
    }
}
