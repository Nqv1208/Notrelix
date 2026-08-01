using System.Reflection;

namespace Notrelix.Domain.Tests.Contracts.Snapshots;

internal static class SnapshotPaths
{
    internal static string GetApprovedReadPath(string fileName)
        => Path.Combine(AppContext.BaseDirectory, "Snapshots", fileName);

    internal static string GetApprovedSourcePath(string fileName)
    {
        var projectDirectory = typeof(SnapshotPaths)
            .Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "DomainTestProjectDirectory")
            .Value
            ?? throw new InvalidOperationException("DomainTestProjectDirectory metadata is missing.");

        return Path.Combine(projectDirectory, "Snapshots", fileName);
    }
}