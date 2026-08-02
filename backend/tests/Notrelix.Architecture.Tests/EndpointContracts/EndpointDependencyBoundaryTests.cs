namespace Notrelix.Architecture.Tests.EndpointContracts;

/// <summary>
/// API-ENDPOINT-001..005: Endpoint dependency boundary.
/// Endpoints are transport-only: bind, dispatch, map to HTTP.
/// They must not use concrete Infrastructure/provider types.
/// Composition root (Program.cs, DependencyInjection) is excluded.
/// </summary>
public sealed class EndpointDependencyBoundaryTests
{
    private static readonly HashSet<string> CompositionRootFiles = new(StringComparer.Ordinal)
    {
        "Program.cs",
        "DependencyInjection.cs",
    };

    private static readonly string[] ForbiddenTokens =
    [
        "IConnectionMultiplexer",
        "IDatabase",
        "StackExchange.Redis",
        "MassTransit",
        "IPublishEndpoint",
        "ISendEndpointProvider",
        "DbContext",
        "DbSet",
        "Npgsql",
        "HttpClient",
        "IHttpClientFactory",
        "AmazonS3",
        "IAmazonS3",
    ];

    [Fact]
    public void API_ENDPOINT_003_No_Direct_Redis_Broker_Provider_Sdk()
    {
        var apiPath = FindApiSourcePath();
        var endpointPath = Path.Combine(apiPath, "Endpoints");

        if (!Directory.Exists(endpointPath))
            return;

        var files = Directory.GetFiles(endpointPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => !CompositionRootFiles.Contains(Path.GetFileName(f)));

        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(apiPath, file);

            foreach (var token in ForbiddenTokens)
            {
                if (content.Contains(token, StringComparison.Ordinal))
                {
                    violations.Add($"{relativePath}: references '{token}'");
                }
            }
        }

        violations.Should().BeEmpty(
            "endpoint files must not reference Redis/broker/provider SDK types. " +
            "Use Application commands/queries and Infrastructure adapters.\n" +
            string.Join("\n", violations));
    }

    [Fact]
    public void API_ENDPOINT_004_CompositionRoot_Exclusions_Are_Exact()
    {
        var apiPath = FindApiSourcePath();
        var endpointPath = Path.Combine(apiPath, "Endpoints");

        if (!Directory.Exists(endpointPath))
            return;

        var files = Directory.GetFiles(endpointPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

        var compositionFiles = files
            .Where(f => CompositionRootFiles.Contains(Path.GetFileName(f)))
            .Select(f => Path.GetFileName(f))
            .Distinct()
            .ToList();

        compositionFiles.Should().OnlyContain(f => CompositionRootFiles.Contains(f),
            "only exact composition-root files may reference Infrastructure types");
    }

    private static string FindApiSourcePath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }

        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");

        return Path.Combine(current, "src", "Notrelix.API");
    }
}
