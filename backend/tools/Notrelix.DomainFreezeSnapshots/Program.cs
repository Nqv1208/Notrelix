using Notrelix.Domain.Tests.Freeze;

if (Environment.GetEnvironmentVariable("CI") == "true")
{
    Console.Error.WriteLine("Snapshot regeneration is forbidden in CI.");
    Environment.Exit(1);
}

var snapshotsDir = Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..",
    "tests", "Notrelix.Domain.Tests", "Snapshots");
snapshotsDir = Path.GetFullPath(snapshotsDir);

Directory.CreateDirectory(snapshotsDir);

WriteSnapshot("DomainEvents.approved.txt", FreezeSnapshotBuilder.BuildDomainEventsSnapshot());
WriteSnapshot("RuleCodes.approved.txt", FreezeSnapshotBuilder.BuildRuleCodesSnapshot());
WriteSnapshot("Enums.approved.txt", FreezeSnapshotBuilder.BuildEnumsSnapshot());
WriteSnapshot("FrozenDomainPublicApi.approved.txt", FreezeSnapshotBuilder.BuildFrozenPublicApiSnapshot());

Console.WriteLine($"Snapshots regenerated in {snapshotsDir}");

void WriteSnapshot(string fileName, string content)
{
    var path = Path.Combine(snapshotsDir, fileName);
    File.WriteAllText(path, content, new System.Text.UTF8Encoding(false));
    Console.WriteLine($"  {fileName}");
}
