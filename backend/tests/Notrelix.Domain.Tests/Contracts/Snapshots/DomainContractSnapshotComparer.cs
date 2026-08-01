using System.Text;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Contracts.Snapshots;

internal static class DomainContractSnapshotComparer
{
    private const string UpdateVariable = "UPDATE_DOMAIN_CONTRACT_SNAPSHOTS";

    internal static void AssertMatchesOrUpdate(
        string snapshotName,
        string generated,
        string approvedFilePath)
    {
        var shouldUpdate = string.Equals(
            Environment.GetEnvironmentVariable(UpdateVariable),
            "1",
            StringComparison.Ordinal);

        var isCi = string.Equals(
            Environment.GetEnvironmentVariable("CI"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (shouldUpdate)
        {
            if (isCi)
            {
                throw new InvalidOperationException(
                    "Domain contract snapshots cannot be updated in CI.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(approvedFilePath)!);

            File.WriteAllText(
                approvedFilePath,
                generated,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            return;
        }

        File.Exists(approvedFilePath)
            .Should()
            .BeTrue($"approved contract snapshot is missing: {approvedFilePath}");

        File.ReadAllText(approvedFilePath)
            .Should()
            .Be(generated, $"Domain contract snapshot '{snapshotName}' changed");
    }
}