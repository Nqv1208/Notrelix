using System.Reflection;
using FluentAssertions;
using Notrelix.Domain.Common;
using Xunit;

namespace Notrelix.Domain.Tests.Freeze.Architecture;

public class TenantScopeTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    [Fact]
    public void TenantScopedEntities_ShouldImplementScopeInterface()
    {
        var violations = new List<string>();

        foreach (var type in DomainAssembly.GetTypes())
        {
            if (!IsDomainType(type)) continue;
            if (type.IsEnum || type.IsInterface || type.IsAbstract) continue;

            if (!typeof(AggregateRoot).IsAssignableFrom(type)) continue;

            // Skip global and hybrid aggregates
            if (DomainCapabilityRegistry.GlobalAggregates.Contains(type.FullName!)) continue;
            if (DomainCapabilityRegistry.HybridAggregates.Contains(type.FullName!)) continue;

            // Check if the aggregate has workspace-scoped properties
            var hasWorkspaceId = type.GetProperty("WorkspaceId") is not null;
            var hasAccountId = type.GetProperty("AccountId") is not null;

            if (hasWorkspaceId || hasAccountId)
            {
                var implementsScope = typeof(IWorkspaceScoped).IsAssignableFrom(type) ||
                                     typeof(IAccountScoped).IsAssignableFrom(type);

                if (!implementsScope)
                {
                    violations.Add($"{type.FullName} (has {(hasWorkspaceId ? "WorkspaceId" : "AccountId")} but doesn't implement scope interface)");
                }
            }
        }

        violations.Should().BeEmpty(
            "tenant-scoped aggregates must implement IWorkspaceScoped or IAccountScoped: " +
            string.Join("\n", violations));
    }

    [Fact]
    public void GlobalAggregates_ShouldNotImplementTenantScope()
    {
        var violations = new List<string>();

        foreach (var aggregateName in DomainCapabilityRegistry.GlobalAggregates)
        {
            var type = DomainAssembly.GetType(aggregateName);
            if (type is null) continue;

            var implementsWorkspace = typeof(IWorkspaceScoped).IsAssignableFrom(type);
            var implementsAccount = typeof(IAccountScoped).IsAssignableFrom(type);

            if (implementsWorkspace || implementsAccount)
            {
                violations.Add($"{type.FullName} is Global but implements scope interface");
            }
        }

        violations.Should().BeEmpty(
            "global aggregates must not implement tenant scope interfaces: " +
            string.Join("\n", violations));
    }

    private static bool IsDomainType(Type type)
    {
        if (type.Namespace is null) return false;
        return type.Namespace.StartsWith("Notrelix.Domain.", StringComparison.Ordinal);
    }
}
