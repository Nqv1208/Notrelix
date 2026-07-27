using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze.Architecture;

public class CommonSharedKernelIsolationTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    [Fact]
    public void CommonAndSharedKernel_ShouldNotDependOnBusinessContexts()
    {
        var commonTypes = DomainAssembly.GetTypes()
            .Where(t => t.Namespace is not null &&
                        t.Namespace.StartsWith("Notrelix.Domain.Common", StringComparison.Ordinal))
            .ToList();

        var sharedKernelTypes = DomainAssembly.GetTypes()
            .Where(t => t.Namespace is not null &&
                        t.Namespace.StartsWith("Notrelix.Domain.SharedKernel", StringComparison.Ordinal))
            .ToList();

        var businessContextPrefixes = new[]
        {
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
        };

        var violations = new List<string>();

        foreach (var type in commonTypes.Concat(sharedKernelTypes))
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var propNs = prop.PropertyType.Namespace;
                if (propNs is not null && businessContextPrefixes.Any(p => propNs.StartsWith(p, StringComparison.Ordinal)))
                {
                    violations.Add($"{type.FullName} -> {prop.PropertyType.FullName}");
                }
            }
        }

        violations.Should().BeEmpty(
            "Common and SharedKernel must not depend on business contexts: " +
            string.Join(", ", violations));
    }
}
