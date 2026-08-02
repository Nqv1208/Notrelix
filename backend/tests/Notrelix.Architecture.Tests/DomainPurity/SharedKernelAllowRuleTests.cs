using System.Reflection;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Architecture.Tests.DomainPurity;

/// <summary>
/// DOM-SHARED-001..002: SharedKernel allow rule.
/// SharedKernel public types must not reference a bounded context namespace,
/// must not be a closed platform resource catalog,
/// and must not have framework/provider dependencies.
/// New SharedKernel public types require architecture review (test change).
/// </summary>
public class SharedKernelAllowRuleTests
{
    private static readonly Assembly DomainAssembly = typeof(ResourceKind).Assembly;

    private static readonly string[] ContextNamespaces =
    [
        "Notrelix.Domain.WorkManagement",
        "Notrelix.Domain.Workspaces",
        "Notrelix.Domain.Identity",
        "Notrelix.Domain.Accounts",
        "Notrelix.Domain.Documents",
        "Notrelix.Domain.Collaboration",
        "Notrelix.Domain.Automation",
        "Notrelix.Domain.Governance",
        "Notrelix.Domain.Integrations",
        "Notrelix.Domain.Billing",
        "Notrelix.Domain.Analytics",
    ];

    private static IEnumerable<Type> GetSharedKernelPublicTypes()
    {
        return DomainAssembly.GetTypes()
            .Where(t => t is { IsPublic: true, IsInterface: false })
            .Where(t => t.Namespace?.StartsWith("Notrelix.Domain.SharedKernel", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void DOM_SHARED_001_SharedKernel_Types_Do_Not_Reference_Context_Namespaces()
    {
        var types = GetSharedKernelPublicTypes();
        var violations = new List<string>();

        foreach (var type in types)
        {
            var referencedTypes = new List<Type>();

            if (type.BaseType is not null && type.BaseType != typeof(object) && type.BaseType != typeof(ValueType))
                referencedTypes.Add(type.BaseType);

            referencedTypes.AddRange(type.GetInterfaces());

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                referencedTypes.Add(prop.PropertyType);

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                referencedTypes.Add(field.FieldType);

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                referencedTypes.Add(method.ReturnType);
                referencedTypes.AddRange(method.GetParameters().Select(p => p.ParameterType));
            }

            foreach (var referenced in referencedTypes)
            {
                var ns = referenced.Namespace;
                if (ns is null) continue;

                foreach (var contextNs in ContextNamespaces)
                {
                    if (ns.StartsWith(contextNs, StringComparison.Ordinal))
                    {
                        violations.Add($"{type.Name} references {referenced.FullName}");
                        break;
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "SharedKernel types must not reference any bounded context namespace");
    }

    [Fact]
    public void DOM_SHARED_002_No_Closed_Platform_Resource_Catalog_Enum()
    {
        var types = GetSharedKernelPublicTypes();

        // ResourceType is the known legacy catalog — frozen at 64 values, being migrated to ResourceKind.
        // No NEW catalog enums may be added.
        var catalogEnums = types
            .Where(t => t.IsEnum)
            .Where(t => t.Name != "ResourceType")
            .Where(t =>
            {
                var values = Enum.GetNames(t);
                return values.Length > 20;
            })
            .Select(t => $"{t.Name} ({Enum.GetNames(t).Length} values)")
            .ToArray();

        catalogEnums.Should().BeEmpty(
            "SharedKernel must not contain closed platform resource catalog enums. " +
            "Use open ResourceKind instead. ResourceType is legacy and must not grow.");
    }

    [Fact]
    public void DOM_SHARED_002_ResourceType_Enum_Is_Legacy_And_Frozen()
    {
        var resourceType = typeof(ResourceType);
        var valueCount = Enum.GetValues<ResourceType>().Length;

        valueCount.Should().Be(64,
            "ResourceType is a legacy frozen enum — no new values may be added. " +
            "New resources use ResourceKind. Current count must remain 64.");
    }
}
