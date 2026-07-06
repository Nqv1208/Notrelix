namespace Notrelix.Architecture.Tests;

public class ApiEndpointMappingArchitectureTests
{
    private static string GetApiPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
            current = Path.GetDirectoryName(current);
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return Path.Combine(current, "src", "Notrelix.API");
    }

    private static string[] GetAllEndpointFiles()
    {
        var apiPath = GetApiPath();
        return Directory.GetFiles(Path.Combine(apiPath, "Endpoints"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
    }

    private static string RemoveComments(string input)
    {
        var blockComments = @"/\*(.*?)\*/";
        var lineComments = @"//(.*?)\r?\n";
        var cleaned = Regex.Replace(input, blockComments, "", RegexOptions.Singleline);
        cleaned = Regex.Replace(cleaned, lineComments, "\n");
        return cleaned;
    }

    private static readonly string[] AllowedRawMapMethods =
    [
        "MapGroup",
        "MapPublicGet", "MapPublicPost", "MapPublicPut", "MapPublicPatch", "MapPublicDelete",
        "MapAuthenticatedGet", "MapAuthenticatedPost", "MapAuthenticatedPut", "MapAuthenticatedPatch", "MapAuthenticatedDelete",
        "MapAccountGet", "MapAccountPost", "MapAccountPut", "MapAccountPatch", "MapAccountDelete",
        "MapWorkspaceGet", "MapWorkspacePost", "MapWorkspacePut", "MapWorkspacePatch", "MapWorkspaceDelete",
        "MapResourceGet", "MapResourcePost", "MapResourcePut", "MapResourcePatch", "MapResourceDelete",
        "MapAdminGet", "MapAdminPost", "MapAdminPut", "MapAdminPatch", "MapAdminDelete",
        "MapInternalGet", "MapInternalPost", "MapInternalPut", "MapInternalPatch", "MapInternalDelete",
    ];

    private static readonly string[] BannedRawMapMethods =
    [
        ".MapGet(", ".MapPost(", ".MapPut(", ".MapPatch(", ".MapDelete(",
    ];

    [Fact]
    public void EndpointFiles_MustNotUseRawMapMethods()
    {
        var files = GetAllEndpointFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (fileName == "EndpointMappingExtensions.cs") continue;

            var content = RemoveComments(File.ReadAllText(file));

            foreach (var banned in BannedRawMapMethods)
            {
                if (content.Contains(banned))
                {
                    violations.Add($"{fileName}: contains {banned}");
                }
            }
        }

        violations.Should().BeEmpty(
            $"Endpoint files must use Map{{Scope}}{{Verb}} DSL methods instead of raw MapGet/MapPost/etc.: " +
            $"{string.Join(", ", violations)}");
    }

    [Fact]
    public void EndpointFiles_MustNotUseMapAsOrTagAs()
    {
        var files = GetAllEndpointFiles();
        var violations = new List<string>();
        var banned = new[]
        {
            ".MapAsPublic(", ".MapAsWorkspaceScoped(", ".MapAsResourceScoped(",
            ".TagAsPublic(", ".TagAsWorkspaceScoped(", ".TagAsResourceScoped(", ".TagAsAdmin(",
        };

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (fileName == "EndpointMappingExtensions.cs") continue;

            var content = RemoveComments(File.ReadAllText(file));
            foreach (var method in banned)
            {
                if (content.Contains(method))
                {
                    violations.Add($"{fileName}: contains {method}");
                }
            }
        }

        violations.Should().BeEmpty(
            $"MapAs*/TagAs* methods are removed; use Map{{Scope}}{{Verb}} DSL: {string.Join(", ", violations)}");
    }

    [Fact]
    public void EndpointMappingExtensions_HasAll35ScopeVerbMethods()
    {
        var file = Path.Combine(GetApiPath(), "Endpoints", "EndpointMappingExtensions.cs");
        var content = RemoveComments(File.ReadAllText(file));

        var expected = new[]
        {
            "MapPublicGet", "MapPublicPost", "MapPublicPut", "MapPublicPatch", "MapPublicDelete",
            "MapAuthenticatedGet", "MapAuthenticatedPost", "MapAuthenticatedPut", "MapAuthenticatedPatch", "MapAuthenticatedDelete",
            "MapAccountGet", "MapAccountPost", "MapAccountPut", "MapAccountPatch", "MapAccountDelete",
            "MapWorkspaceGet", "MapWorkspacePost", "MapWorkspacePut", "MapWorkspacePatch", "MapWorkspaceDelete",
            "MapResourceGet", "MapResourcePost", "MapResourcePut", "MapResourcePatch", "MapResourceDelete",
            "MapAdminGet", "MapAdminPost", "MapAdminPut", "MapAdminPatch", "MapAdminDelete",
            "MapInternalGet", "MapInternalPost", "MapInternalPut", "MapInternalPatch", "MapInternalDelete",
        };

        var missing = new List<string>();
        foreach (var method in expected)
        {
            if (!content.Contains($"public static RouteHandlerBuilder {method}"))
                missing.Add(method);
        }

        missing.Should().BeEmpty($"EndpointMappingExtensions must have all 35 Map{{Scope}}{{Verb}} methods: {string.Join(", ", missing)}");
    }

    [Fact]
    public void EndpointRouteBuilderExtensions_HasNoDsl()
    {
        var file = Path.Combine(GetApiPath(), "Endpoints", "EndpointRouteBuilderExtensions.cs");
        var content = RemoveComments(File.ReadAllText(file));

        var dslUsages = BannedRawMapMethods
            .Where(m => content.Contains(m))
            .ToList();

        dslUsages.Should().BeEmpty(
            "EndpointRouteBuilderExtensions is the composition root and must not contain route registration DSL: " +
            string.Join(", ", dslUsages));
    }
}
