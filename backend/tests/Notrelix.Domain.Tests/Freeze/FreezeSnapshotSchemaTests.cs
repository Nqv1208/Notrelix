using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Verifies the Frozen public API snapshot uses the schema v2 row contract:
/// Type|Member|MemberType|Visibility|IsAbstract|IsVirtual|ReturnOrPropertyType|ParametersOrAccessor.
/// </summary>
public class FreezeSnapshotSchemaTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    private static IEnumerable<string[]> GetDataRows(string snapshot)
    {
        return snapshot
            .Split('\n')
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
            .Select(line => line.Split('|'));
    }

    private static string BuildSnapshot() =>
        FreezeSnapshotBuilder.BuildFrozenPublicApiSnapshot();

    [Fact]
    public void FrozenPublicApiSnapshot_UsesSchema2()
    {
        var snapshot = BuildSnapshot();
        snapshot.Should().Contain("# Snapshot schema: 2");
        snapshot.Should().NotContain("# Snapshot schema: 1");
    }

    [Fact]
    public void FrozenPublicApiSnapshot_EveryDataRowHasEightColumns()
    {
        var snapshot = BuildSnapshot();

        foreach (var row in GetDataRows(snapshot))
        {
            row.Should().HaveCount(8, $"row: {string.Join("|", row)}");
        }
    }

    [Fact]
    public void ConstructorRow_UsesVoidThenParameters()
    {
        var snapshot = BuildSnapshot();
        var ctorRows = GetDataRows(snapshot)
            .Where(row => row.Length == 8 && row[2] == "Constructor")
            .ToList();

        ctorRows.Should().NotBeEmpty("the snapshot must contain constructor rows");

        foreach (var row in ctorRows)
        {
            row[6].Should().Be("System.Void", $"constructor row: {string.Join("|", row)}");
            row[7].Should().NotContain("void", $"constructor signature must use the canonical parameter signature");
            if (row[7].Length > 0)
                row[7].Should().Contain(" ", "parameters must be serialized as 'Type name' pairs");
        }
    }

    [Fact]
    public void MethodRow_UsesReturnTypeThenParameters()
    {
        var snapshot = BuildSnapshot();
        var methodRows = GetDataRows(snapshot)
            .Where(row => row.Length == 8 && row[2] == "Method")
            .ToList();

        methodRows.Should().NotBeEmpty("the snapshot must contain method rows");

        foreach (var row in methodRows)
        {
            row[6].Should().NotBeEmpty("method return type must be present");
            row[6].Should().NotBe("void", "return types must use the canonical name (System.Void, not void)");
        }
    }

    [Fact]
    public void PropertyRow_UsesPropertyTypeThenAccessor()
    {
        var snapshot = BuildSnapshot();
        var propertyRows = GetDataRows(snapshot)
            .Where(row => row.Length == 8 && row[2] == "Property")
            .ToList();

        propertyRows.Should().NotBeEmpty("the snapshot must contain property rows");

        foreach (var row in propertyRows)
        {
            row[6].Should().NotBeEmpty("property type must be present");
            row[7].Should().BeOneOf("readonly", "readwrite", $"property row: {string.Join("|", row)}");
        }
    }

    [Fact]
    public void FrozenPublicApiSnapshot_ExcludesStabilizingCapabilities()
    {
        var snapshot = BuildSnapshot();

        var stabilizingTypes = DomainAssembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsDefined(typeof(ObsoleteAttribute), false))
            .Where(t => t.Namespace is not null &&
                        (t.Namespace.StartsWith("Notrelix.Domain.Collaboration.Reactions", StringComparison.Ordinal) ||
                         t.Namespace.StartsWith("Notrelix.Domain.Collaboration.Watchers", StringComparison.Ordinal) ||
                         t.Namespace.StartsWith("Notrelix.Domain.Automation.Scheduled", StringComparison.Ordinal) ||
                         t.Namespace.StartsWith("Notrelix.Domain.Automation.Templates", StringComparison.Ordinal) ||
                         t.Namespace.StartsWith("Notrelix.Domain.Integrations.Calendar", StringComparison.Ordinal) ||
                         t.Namespace.StartsWith("Notrelix.Domain.Integrations.Webhooks", StringComparison.Ordinal) ||
                         t.Namespace.StartsWith("Notrelix.Domain.Integrations.Sync", StringComparison.Ordinal)))
            .Where(t => DomainCapabilityRegistry.ResolveCapability(t) == DomainCapabilityStatus.Stabilizing)
            .ToList();

        stabilizingTypes.Should().NotBeEmpty("the Domain assembly must contain Stabilizing representative types");

        foreach (var type in stabilizingTypes)
        {
            var rows = GetDataRows(snapshot)
                .Where(row => row.Length == 8 && row[0] == type.FullName)
                .ToList();

            rows.Should().BeEmpty(
                $"Stabilizing type {type.FullName} must not appear in the Frozen public API snapshot");
        }
    }
}
