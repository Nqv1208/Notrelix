namespace Notrelix.Architecture.Tests;

/// <summary>
/// The integration fleet runs the code under test on the real database roles: hosts and pipelines connect as
/// the non-owner application login and background work as the worker login. The owner (container superuser)
/// migrates, applies the policy pack, seeds, resets and verifies. These gates keep a new test from quietly
/// running a host on the owner connection (which bypasses every policy), and keep the set of tests that
/// build their own owner-connected container small and named.
/// </summary>
public class IntegrationFleetRoleBoundaryTests
{
    /// <summary>
    /// Tests that create their own PostgreSQL container on purpose. Each exercises the owner path itself:
    /// migrations and backfills (owner by definition), the policy pack applied to a database of the test's
    /// own making, or a role the test creates to prove a specific policy. Anything else uses the shared
    /// fixtures, which migrate as the owner and hand out the application and worker logins.
    /// </summary>
    private static readonly string[] OwnerContainerTests =
    [
        Path.Combine("Collaboration", "AttachmentObjectLifecycleIntegrationTests.cs"),
        Path.Combine("Integration", "RlsRuntimeEnforcementTests.cs"),
        Path.Combine("WorkManagement", "ItemKeyBackfillIntegrationTests.cs"),
        Path.Combine("WorkManagement", "StatusOptionCategoryBackfillIntegrationTests.cs"),
        Path.Combine("WorkManagement", "StatusCompletionBackfillIntegrationTests.cs"),
        Path.Combine("WorkManagement", "BackfillMigrationGuardIntegrationTests.cs"),
        Path.Combine("WorkManagement", "WorkOrderingPreflightIntegrationTests.cs"),
        Path.Combine("WorkManagement", "WorkChildTableRlsIntegrationTests.cs"),
        Path.Combine("Workspaces", "AccountWorkspacesAndAvatarRlsIntegrationTests.cs"),
        Path.Combine("Containers", "PostgresTestContainer.cs"),
    ];

    /// <summary>Files that name a database connection that is never opened (composition tests with a placeholder).</summary>
    private static readonly string[] PlaceholderConnectionTests =
    [
        Path.Combine("Integration", "ProductionCompositionTests.cs"),
    ];

    private static string IntegrationRoot()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }

        return Path.Combine(
            current ?? throw new DirectoryNotFoundException("Could not find backend.slnx root."),
            "tests", "Notrelix.Integration.Tests");
    }

    private static IEnumerable<(string Relative, string Text)> IntegrationSources()
    {
        var root = IntegrationRoot();
        var sep = Path.DirectorySeparatorChar;
        return Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{sep}obj{sep}") && !f.Contains($"{sep}bin{sep}"))
            .Select(f => (Path.GetRelativePath(root, f), File.ReadAllText(f)));
    }

    [Fact]
    public void NoHostOrPipeline_IsGivenTheOwnerConnectionAsItsApplicationConnection()
    {
        var assignment = new Regex(
            @"""ConnectionStrings:NotrelixDb""\]\s*=\s*(?<value>[^;\r\n]*)", RegexOptions.Compiled);

        var violations = IntegrationSources()
            .SelectMany(source => assignment.Matches(source.Text)
                .Select(m => (source.Relative, Value: m.Groups["value"].Value)))
            .Where(x => x.Value.Contains("Owner", StringComparison.OrdinalIgnoreCase))
            .Select(x => $"{x.Relative}: {x.Value.Trim()}")
            .ToList();

        violations.Should().BeEmpty(
            "a host on the owner connection bypasses every row-level-security policy; hosts take the application login");
    }

    [Fact]
    public void EveryTestThatConfiguresTheApplicationConnection_AlsoConfiguresTheWorkerConnection()
    {
        var missing = IntegrationSources()
            .Where(s => Regex.IsMatch(s.Text, @"""ConnectionStrings:NotrelixDb""\]\s*="))
            .Where(s => !PlaceholderConnectionTests.Contains(s.Relative))
            .Where(s => !s.Text.Contains("ConnectionStrings:NotrelixWorkerDb", StringComparison.Ordinal))
            .Select(s => s.Relative)
            .ToList();

        missing.Should().BeEmpty(
            "background work runs on the worker login; without it the host's role guard refuses to start");
    }

    [Fact]
    public void OnlyTheNamedTests_BuildTheirOwnPostgresContainer()
    {
        var sources = IntegrationSources().ToList();
        var actual = sources
            .Where(s => s.Text.Contains("new PostgreSqlBuilder", StringComparison.Ordinal))
            .Select(s => s.Relative)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        actual.Should().BeEquivalentTo(
            OwnerContainerTests.OrderBy(x => x, StringComparer.Ordinal),
            "tests that run on a container of their own bypass the shared role-split fixture; adding one means naming it here and saying why it must use the owner");
        sources.Select(s => s.Relative).Should().Contain(OwnerContainerTests,
            "every named owner-container test must still exist");
    }
}
