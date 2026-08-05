using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Contracts.Snapshots;

public class DomainContractSnapshotTests
{
    private const string UpdateVariable = "UPDATE_DOMAIN_CONTRACT_SNAPSHOTS";

    private static readonly string[] SnapshotFileNames =
    [
        "DomainEvents.approved.txt",
        "DomainEventEnums.approved.txt",
        "RuleCodes.approved.txt",
    ];

    private static string ResolveApprovedPath(string fileName)
    {
        var shouldUpdate = string.Equals(
            Environment.GetEnvironmentVariable(UpdateVariable),
            "1",
            StringComparison.Ordinal);

        return shouldUpdate
            ? SnapshotPaths.GetApprovedSourcePath(fileName)
            : SnapshotPaths.GetApprovedReadPath(fileName);
    }

    [Fact]
    public void DomainEvents_ShouldNotDrift()
    {
        var generated = DomainContractSnapshotBuilder.BuildDomainEventsSnapshot();
        var approvedPath = ResolveApprovedPath("DomainEvents.approved.txt");
        DomainContractSnapshotComparer.AssertMatchesOrUpdate("DomainEvents", generated, approvedPath);
    }

    [Fact]
    public void RuleCodes_ShouldNotDrift()
    {
        var generated = DomainContractSnapshotBuilder.BuildRuleCodesSnapshot();
        var approvedPath = ResolveApprovedPath("RuleCodes.approved.txt");
        DomainContractSnapshotComparer.AssertMatchesOrUpdate("RuleCodes", generated, approvedPath);
    }

    [Fact]
    public void DomainEventEnums_ShouldNotDrift()
    {
        var generated = DomainContractSnapshotBuilder.BuildDomainEventEnumsSnapshot();
        var approvedPath = ResolveApprovedPath("DomainEventEnums.approved.txt");
        DomainContractSnapshotComparer.AssertMatchesOrUpdate("DomainEventEnums", generated, approvedPath);
    }

    [Fact]
    public void ApprovedReadPathsExistForAllSnapshots()
    {
        foreach (var fileName in SnapshotFileNames)
        {
            var readPath = SnapshotPaths.GetApprovedReadPath(fileName);
            File.Exists(readPath).Should().BeTrue($"approved snapshot output copy is missing: {readPath}");
        }
    }

    [Fact]
    public void ApprovedSourcePathsResolveInsideDomainTestsProject()
    {
        var projectDirectory = Path.GetFullPath(
            typeof(DomainContractSnapshotTests)
                .Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(attribute => attribute.Key == "DomainTestProjectDirectory")
                .Value!);

        foreach (var fileName in SnapshotFileNames)
        {
            var sourceFull = Path.GetFullPath(SnapshotPaths.GetApprovedSourcePath(fileName));
            var relative = Path.GetRelativePath(projectDirectory, sourceFull);

            relative.Should().NotStartWith($"..{Path.DirectorySeparatorChar}", $"approved source path must stay inside the Domain.Tests project: {sourceFull}");
            relative.Should().Be(Path.Combine("Snapshots", fileName), $"approved source path must resolve to Snapshots/{fileName}");
        }
    }

    [Fact]
    public void UpdateModeIsRejectedInCi()
    {
        var previousUpdate = Environment.GetEnvironmentVariable(UpdateVariable);
        var previousCi = Environment.GetEnvironmentVariable("CI");

        try
        {
            Environment.SetEnvironmentVariable(UpdateVariable, "1");
            Environment.SetEnvironmentVariable("CI", "true");

            var action = () => DomainContractSnapshotComparer.AssertMatchesOrUpdate(
                "UpdateModeIsRejectedInCi",
                "content",
                Path.Combine(Path.GetTempPath(), "ShouldNotBeWritten.approved.txt"));

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Domain contract snapshots cannot be updated in CI.");
        }
        finally
        {
            Environment.SetEnvironmentVariable(UpdateVariable, previousUpdate);
            Environment.SetEnvironmentVariable("CI", previousCi);
        }
    }

    [Fact]
    public void NormalModeDoesNotModifyApprovedFiles()
    {
        var previousUpdate = Environment.GetEnvironmentVariable(UpdateVariable);

        try
        {
            Environment.SetEnvironmentVariable(UpdateVariable, null);

            var tempApprovedPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.approved.txt");
            File.WriteAllText(tempApprovedPath, "original");

            try
            {
                var action = () => DomainContractSnapshotComparer.AssertMatchesOrUpdate(
                    "NormalModeDoesNotModifyApprovedFiles",
                    "generated-different-content",
                    tempApprovedPath);

                action.Should().Throw<Exception>();
            }
            finally
            {
                File.ReadAllText(tempApprovedPath).Should().Be("original", "normal comparison mode must never modify approved files");
                File.Delete(tempApprovedPath);
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable(UpdateVariable, previousUpdate);
        }
    }
}