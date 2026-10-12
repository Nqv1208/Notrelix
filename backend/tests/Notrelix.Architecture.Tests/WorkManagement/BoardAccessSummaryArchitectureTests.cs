using System.Reflection;
using System.Text.RegularExpressions;
using Notrelix.Application.Features.Governance.Authorization;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardAccess;

namespace Notrelix.Architecture.Tests.WorkManagement;

/// <summary>
/// The Board access read crosses Work Management and Governance only through
/// published ports: the Work handler asks the Governance access-summary port
/// (never Governance persistence, never its own DbContext), and Governance
/// reads Board members through the Work-published facts port, so its access
/// entries SQL never reads Work tables.
/// </summary>
public sealed class BoardAccessSummaryArchitectureTests
{
    [Fact]
    public void BoardAccessHandler_UsesTheGovernancePort_AndNoDbContext()
    {
        var parameters = ConstructorParameterNames(typeof(GetBoardAccessQueryHandler));

        parameters.Should().Contain("IResourceAccessSummary");
        parameters.Should().Contain("IBoardAccessSubjectFacts");
        parameters.Should().NotContain(["IGovernanceDbContext", "IWorkManagementDbContext", "IWorkspaceDbContext"],
            "a new Application handler must not extend direct EF use (EX-BE-APP-EF-001)");
    }

    [Fact]
    public void AccessSummaryService_ReadsWorkThroughTheWorkPort_AndNoDbContext()
    {
        var parameters = ConstructorParameterNames(typeof(ResourceAccessSummaryService));

        parameters.Should().Contain(["IBoardAccessSubjectFacts", "IAccessFactsProvider", "IAccessPolicyEvaluator"]);
        parameters.Where(name => name.EndsWith("DbContext", StringComparison.Ordinal)).Should().BeEmpty();
    }

    [Fact]
    public void AccessEntriesSql_ReadsNoWorkTables()
    {
        var source = File.ReadAllText(Path.Combine(FindSrcRoot(), "Notrelix.Infrastructure", "Data", "Authz", "ResourceAccessEntriesQuery.cs"));

        source.Should().Contain("governance.resource_permissions", "the scan must inspect the real query (NRX-016)");
        Regex.Matches(source, @"\bwork\.[a-z_]+").Should().BeEmpty(
            "Board members come from the Work-published facts port; the access-facts read of Work tables must not grow");
    }

    private static string[] ConstructorParameterNames(Type type) =>
        type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType.Name)
            .ToArray();

    private static string FindSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "Notrelix.Application")))
        {
            dir = dir.Parent;
        }

        return dir is null
            ? throw new DirectoryNotFoundException("Could not locate backend/src root from test directory")
            : Path.Combine(dir.FullName, "src");
    }
}
