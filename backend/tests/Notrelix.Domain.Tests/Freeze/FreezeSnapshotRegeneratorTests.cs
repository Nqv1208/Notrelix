namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Manual tool to regenerate approved snapshot files.
/// Run via: dotnet test --filter "FreezeSnapshotRegeneratorTests"
/// This is NOT a normal [Fact] - it writes files and should be run intentionally.
/// </summary>
public class FreezeSnapshotRegeneratorTests
{
    private static readonly string SnapshotsDir = GetSnapshotsDirectory();

    private static string GetSnapshotsDirectory()
    {
        var assemblyDir = Path.GetDirectoryName(typeof(FreezeSnapshotRegeneratorTests).Assembly.Location)!;
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
    public void Regenerate_All_Snapshots()
    {
        Directory.CreateDirectory(SnapshotsDir);

        WriteSnapshot("DomainEvents.approved.txt", FreezeSnapshotBuilder.BuildDomainEventsSnapshot());
        WriteSnapshot("RuleCodes.approved.txt", FreezeSnapshotBuilder.BuildRuleCodesSnapshot());
        WriteSnapshot("Enums.approved.txt", FreezeSnapshotBuilder.BuildEnumsSnapshot());
        WriteSnapshot("FrozenDomainPublicApi.approved.txt", FreezeSnapshotBuilder.BuildFrozenPublicApiSnapshot());
    }

    private static void WriteSnapshot(string fileName, string content)
    {
        var path = Path.Combine(SnapshotsDir, fileName);
        File.WriteAllText(path, content, new System.Text.UTF8Encoding(false)); // UTF-8 without BOM
    }
}
