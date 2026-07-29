using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Verifies no drift in Domain freeze contract snapshots.
/// Comparison only - never writes to approved files.
/// Use FreezeSnapshotRegeneratorTests to regenerate.
/// </summary>
public class FreezeSnapshotTests
{
    private static readonly string SnapshotsDir = GetSnapshotsDirectory();

    private static string GetSnapshotsDirectory()
    {
        var assemblyDir = Path.GetDirectoryName(typeof(FreezeSnapshotTests).Assembly.Location)!;
        var dir = new DirectoryInfo(assemblyDir);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Snapshots");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return Path.Combine(assemblyDir, "Snapshots");
    }

    [Fact]
    public void DomainEvents_ShouldNotDrift()
    {
        var generated = FreezeSnapshotBuilder.BuildDomainEventsSnapshot();
        var approvedPath = Path.Combine(SnapshotsDir, "DomainEvents.approved.txt");
        FreezeSnapshotComparer.AssertNoDrift("DomainEvents", generated, approvedPath);
    }

    [Fact]
    public void RuleCodes_ShouldNotDrift()
    {
        var generated = FreezeSnapshotBuilder.BuildRuleCodesSnapshot();
        var approvedPath = Path.Combine(SnapshotsDir, "RuleCodes.approved.txt");
        FreezeSnapshotComparer.AssertNoDrift("RuleCodes", generated, approvedPath);
    }

    [Fact]
    public void Enums_ShouldNotDrift()
    {
        var generated = FreezeSnapshotBuilder.BuildEnumsSnapshot();
        var approvedPath = Path.Combine(SnapshotsDir, "Enums.approved.txt");
        FreezeSnapshotComparer.AssertNoDrift("Enums", generated, approvedPath);
    }

    [Fact]
    public void FrozenPublicApi_ShouldNotDrift()
    {
        var generated = FreezeSnapshotBuilder.BuildFrozenPublicApiSnapshot();
        var approvedPath = Path.Combine(SnapshotsDir, "FrozenDomainPublicApi.approved.txt");
        FreezeSnapshotComparer.AssertNoDrift("FrozenPublicApi", generated, approvedPath);
    }

    [Fact]
    public void SnapshotRegenerator_ShouldNotBeDiscoverable()
    {
        var testTypes = typeof(FreezeSnapshotTests).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true })
            .ToList();

        var regenerators = testTypes
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(m => new { Type = t, Method = m }))
            .Where(x => x.Method.Name.StartsWith("Regenerate", StringComparison.Ordinal))
            .ToList();

        regenerators.Should().BeEmpty(
            "snapshot regenerator should not be a discoverable test. " +
            "Use tools/Notrelix.DomainFreezeSnapshots instead.");
    }
}
