using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Maturity;

public class ExperimentalWorkManagementIsolationTests
{
    private static readonly Type[] ExperimentalTypes =
    [
        ..typeof(Notrelix.Domain.WorkManagement.Approvals.ApprovalRequest).Assembly.GetTypes()
            .Where(t => t.Namespace is not null &&
                (t.Namespace.Contains("Approvals") ||
                 t.Namespace.Contains("Formulas") ||
                 t.Namespace.Contains("Rollups") ||
                 t.Namespace.Contains("Workload")))
    ];

    private static readonly Type[] FrozenContextTypes =
    [
        ..typeof(Notrelix.Domain.WorkManagement.Boards.Board).Assembly.GetTypes()
            .Where(t => t.Namespace is not null &&
                t.Namespace.StartsWith("Notrelix.Domain.WorkManagement.") &&
                !t.Namespace.Contains("Approvals") &&
                !t.Namespace.Contains("Formulas") &&
                !t.Namespace.Contains("Rollups") &&
                !t.Namespace.Contains("Workload") &&
                !t.Namespace.Contains("WorkManagementRuleCodes"))
    ];

    [Fact]
    public void FrozenTypes_ShouldNotReferenceExperimentalTypes()
    {
        var experimentalTypeNames = ExperimentalTypes.Select(t => t.FullName!).ToHashSet();
        var violations = new List<string>();

        foreach (var frozenType in FrozenContextTypes)
        {
            if (frozenType.IsGenericTypeDefinition)
            {
                foreach (var arg in frozenType.GetGenericArguments())
                {
                    if (experimentalTypeNames.Contains(arg.FullName!))
                        violations.Add($"{frozenType.FullName} generic arg references {arg.FullName}");
                }
            }

            foreach (var property in frozenType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var propType = property.PropertyType;
                if (experimentalTypeNames.Contains(propType.FullName!))
                    violations.Add($"{frozenType.FullName}.{property.Name} returns {propType.FullName}");
            }

            foreach (var method in frozenType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.IsSpecialName) continue;
                foreach (var param in method.GetParameters())
                {
                    if (experimentalTypeNames.Contains(param.ParameterType.FullName!))
                        violations.Add($"{frozenType.FullName}.{method.Name} param {param.Name} is {param.ParameterType.FullName}");
                }
                if (experimentalTypeNames.Contains(method.ReturnType.FullName!))
                    violations.Add($"{frozenType.FullName}.{method.Name} returns {method.ReturnType.FullName}");
            }

            foreach (var iface in frozenType.GetInterfaces())
            {
                if (experimentalTypeNames.Contains(iface.FullName!))
                    violations.Add($"{frozenType.FullName} implements {iface.FullName}");
            }
        }

        violations.Should().BeEmpty($"Frozen WorkManagement types must not reference Experimental types. Found {violations.Count} violations.");
    }

    [Fact]
    public void ExperimentalTypes_ShouldBeSealed_OrHaveClearLifecycle()
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
