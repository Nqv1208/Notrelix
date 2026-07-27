using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Compares generated snapshot content against approved files.
/// Used in CI tests to detect drift. Never writes to approved files.
/// </summary>
public static class FreezeSnapshotComparer
{
    public static void AssertNoDrift(string snapshotName, string generated, string approvedFilePath)
    {
        if (!File.Exists(approvedFilePath))
        {
            Assert.Fail($"Approved snapshot not found: {approvedFilePath}. Run FreezeSnapshotRegenerator to create it.");
            return;
        }

        var approved = File.ReadAllText(approvedFilePath);

        generated.Should().Be(approved,
            $"snapshot '{snapshotName}' has drifted. " +
            $"Run FreezeSnapshotRegenerator to update the approved file if the change is intentional.");
    }
}
