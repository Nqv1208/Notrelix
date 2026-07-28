namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Manual tool to regenerate approved snapshot files.
/// Run via: UPDATE_DOMAIN_FREEZE_SNAPSHOTS=1 dotnet test --filter "FreezeSnapshotRegeneratorTests"
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
    [Trait("Category", "ManualSnapshotUpdate")]
    public void Regenerate_All_Snapshots()
    {
        if (Environment.GetEnvironmentVariable("UPDATE_DOMAIN_FREEZE_SNAPSHOTS") != "1")
            return; // Manual snapshot update only. Set UPDATE_DOMAIN_FREEZE_SNAPSHOTS=1 to run.

        if (Environment.GetEnvironmentVariable("CI") == "true")
            throw new InvalidOperationException(
                "Snapshot regeneration is forbidden in CI.");

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
