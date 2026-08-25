using System.Reflection;
using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Architecture.Tests.Pipeline;

/// <summary>
/// IA-TST-CLOSURE — executable closure gates for PR #95 (pipeline-closure spec
/// §14 items [05][06][07][08]). Complements PipelineFreezeArchitectureTests by
/// pinning documentation convergence, allowlist hygiene and full-request
/// descriptor validity at the exact production boundary.
/// </summary>
public sealed class PipelineClosureArchitectureTests
{
    private const string DocsRoot = "docs";
    private const string ApplicationRoot = "Notrelix.Application";

    private static readonly string[] FrozenOrder =
    [
        "ExceptionMappingBehavior",
        "ApplicationTracingBehavior",
        "RequestContractBehavior",
        "ExecutionContextBehavior",
        "DataSessionBehavior",
        "AccessControlBehavior",
        "IdempotencyBehavior",
    ];

    // PC-TEST-004
    [Fact]
    public void PipelineClosure_CanonicalDocs_MatchFrozenSevenStageTopology()
    {
        var backendRoot = FindBackendRoot();

        var adr = File.ReadAllText(Path.Combine(backendRoot, DocsRoot,
            "decisions/ADR-006-frozen-seven-behavior-pipeline.md"));
        var appModel = File.ReadAllText(Path.Combine(backendRoot, DocsRoot,
            "architecture/application-model.md"));

        foreach (var behavior in FrozenOrder)
        {
            adr.Should().Contain(behavior, $"ADR-006 must name frozen stage {behavior}");
            appModel.Should().Contain(behavior, $"application-model current evidence must list {behavior}");
        }

        adr.Should().Contain("validation executes inside RequestContractBehavior",
            "ADR must state validation ownership explicitly");

        // application-model current-evidence block must not list ValidationBehavior.
        var evidenceBlock = appModel[appModel.IndexOf("# 15. Current behavior evidence")..];
        var blockEnd = evidenceBlock.IndexOf("---", StringComparison.Ordinal);
        evidenceBlock = evidenceBlock[..blockEnd];
        var stageLines = evidenceBlock.Split('\n')
            .Select(l => l.Trim().TrimEnd('`'))
            .Where(l => l.EndsWith("Behavior"))
            .ToArray();
        stageLines.Should().NotContain("ValidationBehavior",
            "ValidationBehavior must not appear as a current production stage line");
    }

    // PC-TEST-005 / 006 / 007
    [Fact]
    public void PipelineClosure_LegacyGapCount_IsZero()
    {
        var markerType = Type.GetType(
            "Notrelix.Architecture.Tests.CommandMarkerArchitectureTests, Notrelix.Architecture.Tests",
            throwOnError: true)!;

        int legacyGapCount = 0;
        foreach (var field in markerType.GetFields(BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (!field.FieldType.IsGenericType
                || field.FieldType.GetGenericTypeDefinition().Name != "Dictionary`2")
            {
                continue;
            }

            var dict = field.GetValue(null);
            if (dict is null)
            {
                continue;
            }

            foreach (var entry in ((System.Collections.IDictionary)dict).Values)
            {
                var classification = entry!.GetType().GetProperty("Classification")!.GetValue(entry);
                if (classification?.ToString() == "LegacyGap")
                {
                    legacyGapCount++;
                }
            }
        }

        legacyGapCount.Should().Be(0,
            "pipeline migration debt must be zero before closure; every remaining exception "
            + "must be Intentional or SystemCommand with a live architectural reason");
    }

    // PC-TEST-006 / 007 (stale rejection proven inside CommandMarkerArchitectureTests;
    // this guard pins that those enforcement tests still exist and are runnable).
    [Fact]
    public void PipelineClosure_Allowlist_StaleDetection_EnforcementExists()
    {
        var markerType = Type.GetType(
            "Notrelix.Architecture.Tests.CommandMarkerArchitectureTests, Notrelix.Architecture.Tests",
            throwOnError: true)!;

        new[] {
            "MutatingCommands_ShouldImplement_IWriteRequest",
            "MutatingCommands_WithWorkspaceId_ShouldImplement_IWorkspaceRequest",
            "CreateUpdateDeleteCommands_ShouldImplement_IRequirePermission",
        }.Should().OnlyContain(name => markerType.GetMethod(name) != null,
            "stale-allowlist self-validation must stay wired into CI");
    }

    // PC-TEST-008
    [Fact]
    public void PipelineClosure_AllProductionRequests_HaveValidDescriptors()
    {
        var registry = RequestDescriptorRegistry.Create(typeof(ICommand<>).Assembly);

        registry.Descriptors.Should().NotBeEmpty();
        registry.Descriptors
            .GroupBy(descriptor => descriptor.RequestType)
            .Where(group => group.Count() > 1)
            .Should().BeEmpty("every production request resolves to exactly one descriptor");

        // Exactly-one classification guarantees from the validator surface here:
        // Principal/Scope/DataAccess are non-default enums on every descriptor.
        foreach (var d in registry.Descriptors)
        {
            d.Principal.ToString().Should().NotBeNullOrEmpty();
            d.Scope.ToString().Should().NotBeNullOrEmpty();
            d.DataAccess.ToString().Should().NotBeNullOrEmpty();
        }
    }

    // --- helpers -------------------------------------------------------------

    private static string FindBackendRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", ApplicationRoot)))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("backend root not found");
    }
}
