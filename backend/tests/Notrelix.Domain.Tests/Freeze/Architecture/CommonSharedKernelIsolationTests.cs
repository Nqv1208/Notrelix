using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze.Architecture;

public class CommonSharedKernelIsolationTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    private static readonly string[] BusinessContextPrefixes =
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

    [Fact]
    public void CommonAndSharedKernel_ShouldNotDependOnBusinessContexts()
    {
        var infrastructurePrefixes = new[]
        {
            "Notrelix.Domain.Common",
            "Notrelix.Domain.SharedKernel",
        };

        var infrastructureTypes = DomainAssembly.GetTypes()
            .Where(t => t.Namespace is not null &&
                        infrastructurePrefixes.Any(p => t.Namespace.StartsWith(p, StringComparison.Ordinal)))
            .ToList();

        var violations = new List<string>();

        foreach (var type in infrastructureTypes)
        {
            var referencedTypes = DomainTypeGraphWalker.GetReferencedTypes(type);

            foreach (var referencedType in referencedTypes)
            {
                var ns = referencedType.Namespace;
                if (ns is not null && BusinessContextPrefixes.Any(p => ns.StartsWith(p, StringComparison.Ordinal)))
                {
                    violations.Add($"{type.FullName} -> {referencedType.FullName}");
                }
            }
        }

        violations.Should().BeEmpty(
            "Common and SharedKernel must not depend on business contexts: " +
            string.Join(", ", violations));
    }
}
