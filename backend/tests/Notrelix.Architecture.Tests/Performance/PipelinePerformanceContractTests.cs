using Notrelix.Application.Common.Security;

namespace Notrelix.Architecture.Tests.Performance;

/// <summary>
/// IA-TST-PERF-STRUCT — deterministic structural performance contract for the
/// frozen pipeline (ADR-006 / freeze file 04 §10 Tier A). Enforced in normal CI:
/// these invariants bound datastore work per request without asserting latency.
/// </summary>
public sealed class PipelinePerformanceContractTests
{
    private const string ApplicationRoot = "Notrelix.Application";
    private const string InfrastructureRoot = "Notrelix.Infrastructure";

    [Fact]
    public void AccessPolicyEngine_PerformsZeroDatastoreOrNetworkIo()
    {
        var engineType = typeof(AccessPolicyEngine);

        engineType.GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Should().BeEmpty("the policy evaluator must be pure — no injected I/O services");

        var source = SourceOf("Notrelix.Application.Common.Security.AccessPolicyEngine");
        source.Should().NotContain("DbContext");
        source.Should().NotContain("HttpClient");
        source.Should().NotContain("ExecuteSql");
        source.Should().NotContain("QueryAsync");
    }

    [Fact]
    public void AccessFactsProvider_IssuesExactlyOneDatastoreCommand()
    {
        var source = SourceOf("Notrelix.Infrastructure.Data.Authz.PostgresAccessFactsProvider");

        // One SQL literal + one ExecuteReaderAsync: the canonical single-command shape.
        Count(source, "ExecuteReaderAsync").Should().Be(1,
            "AccessFacts resolution must be exactly one datastore command");
        Count(source, "ExecuteSqlRaw").Should().Be(0);
        Count(source, "CommandText ==").Should().Be(0, "no branching into additional commands");

        var constCount = Count(source, "\"\"\"");
        constCount.Should().BeGreaterThanOrEqualTo(2, "the facts query stays one verbatim SQL block");
    }

    [Fact]
    public void ResourceLocator_ResolvesEachResourceWithASingleQuery()
    {
        var source = SourceOf("Notrelix.Infrastructure.Services.ResourceLocator");

        Count(source, ".ToListAsync(").Should().Be(0,
            "resource resolution materializes at most one row, never lists");
        Count(source, "Include(").Should().Be(0, "locator returns immutable minimum metadata only");
        Count(source, "FirstOrDefaultAsync(").Should().BeGreaterThan(0);
    }

    [Fact]
    public void BusinessTransaction_ContainsNoExternalNetworkCalls()
    {
        var sessionSource = SourceOf("Notrelix.Infrastructure.Data.EfRequestDataSession");

        sessionSource.Should().NotContain("HttpClient");
        sessionSource.Should().NotContain("IPublishEndpoint");
        sessionSource.Should().NotContain("IRealtimePublisher");
        sessionSource.Should().NotContain("IN8nClient");
        sessionSource.Should().NotContain("_publishEndpoint");

        var behaviorFolderSources = SourcesUnder("Notrelix.Application.Common.Behaviors");
        behaviorFolderSources.Should().NotContain("HttpClient");
        behaviorFolderSources.Should().NotContain("IPublishEndpoint");
        behaviorFolderSources.Should().NotContain("IRealtimePublisher");
        behaviorFolderSources.Should().NotContain("IN8nClient",
            "external effects belong to post-commit broker consumers, never pipeline behaviors");
    }

    // --- helpers -------------------------------------------------------------

    private static readonly string BackendRoot = FindBackendRoot();

    private static string FindBackendRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", ApplicationRoot)))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new DirectoryNotFoundException("Could not locate backend/src root from test directory");
        }

        return dir.FullName;
    }

    private static string SourceOf(string fullTypeName)
    {
        var rootNamespace = fullTypeName.StartsWith(ApplicationRoot, StringComparison.Ordinal)
            ? ApplicationRoot
            : InfrastructureRoot;
        var relative = fullTypeName[rootNamespace.Length..]
            .TrimStart('.')
            .Replace('.', '/').Replace("+", "/");
        foreach (var root in new[] { ApplicationRoot, InfrastructureRoot })
        {
            foreach (var extension in new[] { ".cs", "" })
            {
                var candidate = Path.Combine(BackendRoot, "src", root, $"{relative}{extension}.cs");
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                candidate = Path.Combine(BackendRoot, "src", root, $"{relative}.cs");
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }
            }
        }

        throw new FileNotFoundException($"Source file not found for {fullTypeName}");
    }

    private static string SourcesUnder(string folderSuffix)
    {
        var dir = Path.Combine(BackendRoot, "src", ApplicationRoot,
            folderSuffix.Replace("Notrelix.Application.", "").Replace('.', '/'));
        return string.Join("\n", Directory.EnumerateFiles(dir, "*.cs").Select(File.ReadAllText));
    }

    private static int Count(string haystack, string needle) =>
        haystack.Split(needle).Length - 1;
}
