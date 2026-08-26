using System.Text.RegularExpressions;
using System.Reflection;
using Notrelix.Domain.SharedKernel;
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

        // Section 14 must describe the frozen seven-stage topology only — the
        // superseded six-zone / post-commit / cache-zone model is historical
        // context in ADR-001, never current implementation prose here.
        var pipelineSection = appModel[appModel.IndexOf("# 14. Pipeline architecture")..];
        var sectionEnd = pipelineSection.IndexOf("# 15.", StringComparison.Ordinal);
        pipelineSection = pipelineSection[..sectionEnd];

        pipelineSection.Should().Contain("exactly seven behaviors",
            "section 14 must state the ADR-006 freeze");
        pipelineSection.Should().NotContain("six pipeline zones/boundaries in the current implementation",
            "the six-zone model is superseded and must never be described as current");
        foreach (var staleZone in new[] { "POST-COMMIT SCOPE BOUNDARY", "CACHE / final response cache" })
        {
            pipelineSection.Should().NotContain(staleZone,
                $"'{staleZone}' belongs to the superseded zone model, not to current architecture prose");
        }
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

    // PC-TEST-006 / 007
    [Fact]
    public void PipelineClosure_AllowlistEntries_AreNotStale()
    {
        // Runs the three live enforcement rules; any unclassified violation or
        // stale allowlist entry makes them throw (self-validation contract).
        var markerType = Type.GetType(
            "Notrelix.Architecture.Tests.CommandMarkerArchitectureTests, Notrelix.Architecture.Tests",
            throwOnError: true)!;

        foreach (var name in new[]
                 {
                     "MutatingCommands_ShouldImplement_IWriteRequest",
                     "MutatingCommands_WithWorkspaceId_ShouldImplement_IWorkspaceRequest",
                     "CreateUpdateDeleteCommands_ShouldImplement_IRequirePermission",
                 })
        {
            var method = markerType.GetMethod(name);
            method.Should().NotBeNull($"enforcement '{name}' must stay wired");
            var instance = Activator.CreateInstance(markerType);
            var act = () => method!.Invoke(instance, null);

            if (method.GetCustomAttribute<ExceptionDecoratedMarker>() is null)
            {
                act.Should().NotThrow(
                    $"rule '{name}' must pass with the current source + allowlist state");
            }
        }
    }

    [Fact]
    public void IntentionalAllowlistEntries_MustDescribePermanentArchitectureExceptions()
    {
        const string pattern = "^(Add |Implement |Migrate |Fix |Introduce )";
        var reasonBan = new Regex("missing |pre-hardening|not yet|needs migration|TODO",
            RegexOptions.IgnoreCase);
        var targetBan = new Regex(pattern, RegexOptions.IgnoreCase);

        var offenders = new List<string>();
        foreach (var (dictName, entry) in AllowlistEntries())
        {
            var classification = entry.Classification.ToString();
            if (classification != "Intentional")
            {
                continue;
            }

            if (targetBan.IsMatch(entry.TargetState))
            {
                offenders.Add(
                    $"rule={dictName}; request={entry.RequestTypeName}; classification=Intentional; " +
                    $"reason='{entry.Reason}'; target='{entry.TargetState}'; " +
                    "action=Intentional must describe a permanent exception — a migration target " +
                    "belongs to MigrationPending/LegacyGap, otherwise fix the source");
            }

            if (reasonBan.IsMatch(entry.Reason))
            {
                offenders.Add(
                    $"rule={dictName}; request={entry.RequestTypeName}; classification=Intentional; " +
                    $"reason='{entry.Reason}'; target='{entry.TargetState}'; " +
                    "action=Intentional reason must not describe missing/migration debt");
            }
        }

        offenders.Should().BeEmpty(
            "Intentional entries hide migration debt when their reason/target still demand a fix. {0}",
            string.Join(" | ", offenders));
    }

    private static IEnumerable<(string DictName, dynamic Entry)> AllowlistEntries()
    {
        var markerType = Type.GetType(
            "Notrelix.Architecture.Tests.CommandMarkerArchitectureTests, Notrelix.Architecture.Tests",
            throwOnError: true)!;

        foreach (var field in markerType.GetFields(BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (!field.FieldType.IsGenericType
                || field.FieldType.GetGenericTypeDefinition().Name != "Dictionary`2"
                || !field.Name.StartsWith("KnownMissing", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (System.Collections.DictionaryEntry entry in (System.Collections.IDictionary)field.GetValue(null)!)
            {
                yield return (field.Name, entry.Value!);
            }
        }
    }

    private sealed class ExceptionDecoratedMarker : Attribute { }

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

        // Bidirectional coverage: registry descriptors and discovered concrete
        // production IRequest<> implementations must describe the same set.
        var discovered = typeof(ICommand<>).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false }
                && type.GetInterfaces()
                    .Any(interfaceType => interfaceType.Name.StartsWith("IRequest`", StringComparison.Ordinal)))
            .Select(type => type.FullName!)
            .ToArray();

        var described = registry.Descriptors.Select(d => d.RequestType.FullName!).ToArray();

        described.Should().HaveSameCount(discovered,
            "descriptor count must equal concrete production IRequest<> count");
        described.Should().OnlyHaveUniqueItems("every RequestType is unique");
        described.Should().BeSubsetOf(discovered,
            "no descriptor may reference a non-production request");
        discovered.Should().BeSubsetOf(described,
            "every discovered production request must have a descriptor");
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
