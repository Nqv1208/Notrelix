using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.SharedKernel;

/// <summary>
/// Verifies that SharedKernel types remain independent of bounded contexts.
/// No SharedKernel type should reference concrete bounded context types.
/// </summary>
public class SharedKernelContextIsolationTests
{
    private static readonly HashSet<string> BoundedContextNamespaces = new()
    {
        "Notrelix.Domain.Accounts",
        "Notrelix.Domain.Identity",
        "Notrelix.Domain.Workspaces",
        "Notrelix.Domain.WorkManagement",
        "Notrelix.Domain.Documents",
        "Notrelix.Domain.Collaboration",
        "Notrelix.Domain.Automation",
        "Notrelix.Domain.Integrations",
        "Notrelix.Domain.Billing",
        "Notrelix.Domain.Governance",
        "Notrelix.Domain.Analytics",
    };

    [Fact]
    public void SharedKernel_Types_DoNotReferenceBoundedContexts()
    {
        var sharedKernelTypes = typeof(SharedKernelRuleCodes).Assembly
            .GetTypes()
            .Where(t => t.Namespace?.StartsWith("Notrelix.Domain.SharedKernel") == true
                        && !t.IsAbstract
                        && !t.IsInterface)
            .ToList();

        var violations = new List<string>();

        foreach (var type in sharedKernelTypes)
        {
            // Check base types
            if (type.BaseType is not null &&
                BoundedContextNamespaces.Any(ns => type.BaseType.Namespace?.StartsWith(ns) == true))
            {
                violations.Add($"{type.Name} inherits from {type.BaseType.Name} (bounded context)");
            }

            // Check implemented interfaces
            foreach (var iface in type.GetInterfaces())
            {
                if (BoundedContextNamespaces.Any(ns => iface.Namespace?.StartsWith(ns) == true))
                {
                    violations.Add($"{type.Name} implements {iface.Name} (bounded context)");
                }
            }

            // Check fields
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (BoundedContextNamespaces.Any(ns => field.FieldType.Namespace?.StartsWith(ns) == true))
                {
                    violations.Add($"{type.Name}.{field.Name} has type {field.FieldType.Name} (bounded context)");
                }
            }

            // Check properties
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var prop in properties)
            {
                if (BoundedContextNamespaces.Any(ns => prop.PropertyType.Namespace?.StartsWith(ns) == true))
                {
                    violations.Add($"{type.Name}.{prop.Name} has type {prop.PropertyType.Name} (bounded context)");
                }
            }
        }

        violations.Should().BeEmpty(
            "SharedKernel types must not reference bounded context types: " +
            string.Join("; ", violations));
    }

    [Fact]
    public void SharedKernel_Dependencies_AreMinimal()
    {
        var sharedKernelAssembly = typeof(SharedKernelRuleCodes).Assembly;
        var referencedAssemblies = sharedKernelAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(name => name?.StartsWith("Notrelix") == true)
            .ToList();

        var violations = referencedAssemblies
            .Where(name => name != "Notrelix.Domain")
            .ToList();

        violations.Should().BeEmpty(
            "SharedKernel should only depend on Notrelix.Domain (common), not other assemblies: " +
            string.Join(", ", violations));
    }
}
