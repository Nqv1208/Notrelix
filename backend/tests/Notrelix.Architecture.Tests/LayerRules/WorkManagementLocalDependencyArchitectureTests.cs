namespace Notrelix.Architecture.Tests.LayerRules;

/// <summary>
/// TAC-WM-001: standard WorkManagement handlers own their scope, auth,
/// idempotency and write-transaction concerns through the canonical pipeline.
/// No WorkManagement handler may inject a foreign bounded-context DbContext
/// for those pipeline-owned concerns; a new real use-case-specific fact would
/// need its own separately classified cross-context contract.
/// </summary>
public class WorkManagementLocalDependencyArchitectureTests
{
    private static readonly string[] ForeignContextDependencies =
    [
        "IWorkspaceDbContext",
        "IGovernanceDbContext",
        "IBillingDbContext",
        "IAccountDbContext",
        "IIdentityDbContext",
        "ICollaborationDbContext",
        "IAutomationDbContext",
        "IIntegrationDbContext",
    ];

    [Fact]
    public void WorkManagementHandlers_MustNotInject_ForeignContextDependencies()
    {
        var featureRoot = FindWorkManagementFeatureRoot();
        var violations = new List<string>();

        foreach (var file in Directory.GetFiles(featureRoot, "*.cs", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            if (!content.Contains("IRequestHandler<"))
            {
                continue;
            }

            var relative = Path.GetRelativePath(featureRoot, file);
            foreach (var foreign in ForeignContextDependencies)
            {
                if (content.Contains(foreign))
                {
                    violations.Add($"{relative}: injects foreign {foreign}");
                }
            }
        }

        violations.Should().BeEmpty(
            "TAC-WM-001: WorkManagement handlers must not inject foreign Workspace/Governance/Billing " +
            "DbContext dependencies for standard pipeline-owned concerns. Violations:\n" +
            string.Join("\n", violations));
    }

    private static string FindWorkManagementFeatureRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "backend.slnx")))
        {
            dir = dir.Parent!;
        }

        var root = dir?.FullName ?? throw new InvalidOperationException("Could not find project root");
        return Path.Combine(root, "src", "Notrelix.Application", "Features", "WorkManagement");
    }
}