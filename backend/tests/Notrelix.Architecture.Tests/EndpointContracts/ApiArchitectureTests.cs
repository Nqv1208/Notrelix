namespace Notrelix.Architecture.Tests;

public class ApiArchitectureTests
{
    private static string GetApiPath()
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

    private static string[] GetEndpointFiles()
    {
        var apiPath = GetApiPath();
        return Directory.GetFiles(Path.Combine(apiPath, "Endpoints"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
    }

    private static string[] GetCsFiles()
    {
        var apiPath = GetApiPath();
        return Directory.GetFiles(apiPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
    }

    private static readonly HashSet<string> DomainNamespaceAllowlist =
    [
        "CreateBlockEndpoint.cs",
        "CreateCommentEndpoint.cs",
        "CreateShareLinkEndpoint.cs",
        "DisableShareLinkEndpoint.cs",
        "GetCommentsEndpoint.cs",
        "GetResourceActivityEndpoint.cs",
        "GetResourcePermissionsEndpoint.cs",
        "GrantResourcePermissionEndpoint.cs",
        "InviteMemberEndpoint.cs",
        "RevokeResourcePermissionEndpoint.cs",
        "SaveBoardViewEndpoint.cs",
        "CreateBoardEndpoint.cs",
        "UpdateMemberRoleEndpoint.cs",
    ];

    [Fact]
    public void EndpointFiles_ShouldNotReference_DomainNamespace()
    {
        var files = GetEndpointFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (DomainNamespaceAllowlist.Contains(fileName)) continue;
            if (file.Contains("Map") && file.EndsWith("Endpoints.cs")) continue;

            var content = RemoveComments(File.ReadAllText(file));

            if (content.Contains("using Notrelix.Domain"))
            {
                violations.Add(fileName);
            }
        }

        violations.Should().BeEmpty($"Endpoint files must not reference Domain namespace: {string.Join(", ", violations)}");
    }

    [Fact]
    public void EndpointFiles_ShouldNotReference_EntityFrameworkCore()
    {
        var files = GetEndpointFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));

            if (content.Contains("using Microsoft.EntityFrameworkCore"))
            {
                violations.Add(Path.GetFileName(file));
            }
        }

        violations.Should().BeEmpty($"Endpoint files must not reference EF Core: {string.Join(", ", violations)}");
    }

    [Fact]
    public void EndpointFiles_ShouldNotInject_DbContext()
    {
        var files = GetEndpointFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            if (!file.Contains("Endpoint.cs")) continue;

            var content = RemoveComments(File.ReadAllText(file));

            if (content.Contains("IApplicationDbContext") ||
                content.Contains("ApplicationDbContext") ||
                content.Contains("DbContext "))
            {
                violations.Add(Path.GetFileName(file));
            }
        }

        violations.Should().BeEmpty($"Endpoint files must not inject DbContext: {string.Join(", ", violations)}");
    }

    [Fact]
    public void EndpointFiles_ShouldBe_UnderBoundedContextModuleStructure()
    {
        var apiPath = GetApiPath();
        var endpointsDir = Path.Combine(apiPath, "Endpoints");
        var dirs = Directory.GetDirectories(endpointsDir);

        var expectedBcs = new[]
        {
            "Collaboration",
            "Documents",
            "Governance",
            "Health",
            "Identity",
            "WorkManagement",
            "Workspaces",
        };

        foreach (var bc in expectedBcs)
        {
            dirs.Should().Contain(d => Path.GetFileName(d) == bc,
                $"Expected bounded context directory '{bc}' to exist under Endpoints/");
        }
    }

    [Fact]
    public void MapFiles_ShouldContain_RouteComposition()
    {
        var apiPath = GetApiPath();
        var mapFiles = Directory.GetFiles(Path.Combine(apiPath, "Endpoints"), "Map*Endpoints.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        foreach (var file in mapFiles)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var hasMapGroup = content.Contains("MapGroup");

            hasMapGroup.Should().BeTrue($"Map file {Path.GetFileName(file)} should call MapGroup for route groups");
        }
    }

    [Fact]
    public void NoControllerBase_Or_ApiController_Used()
    {
        var files = GetCsFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));

            if (content.Contains("ControllerBase") || content.Contains("[ApiController]"))
            {
                violations.Add(Path.GetFileName(file));
            }
        }

        violations.Should().BeEmpty($"API must not use ControllerBase or [ApiController]: {string.Join(", ", violations)}");
    }

    [Fact]
    public void EndpointHandlerMethods_ShouldReturn_IResult()
    {
        var endpointFiles = Directory.GetFiles(GetApiPath(), "*Endpoint.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var violations = new List<string>();

        foreach (var file in endpointFiles)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var lines = content.Split('\n');

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("private static") && !trimmed.StartsWith("public static"))
                    continue;
                if (!trimmed.Contains(" async Task<") && !trimmed.Contains(" Task<IResult>"))
                    continue;
                if (trimmed.Contains("IResult")) continue;

                violations.Add($"{Path.GetFileName(file)}: {trimmed}");
            }
        }

        violations.Should().BeEmpty($"Endpoint handler methods should return IResult: {string.Join(", ", violations)}");
    }

    [Fact]
    public void EndpointAccessAttributes_Exist()
    {
        var securityDir = Path.Combine(GetApiPath(), "Security");
        var file = Directory.GetFiles(securityDir, "*.cs").FirstOrDefault();
        file.Should().NotBeNull("Security directory must contain endpoint access attributes");
        var content = File.ReadAllText(file!);

        content.Should().Contain("class PublicEndpointAttribute", "PublicEndpoint attribute must exist");
        content.Should().Contain("class WorkspaceScopedEndpointAttribute", "WorkspaceScopedEndpoint attribute must exist");
        content.Should().Contain("class ResourceScopedEndpointAttribute", "ResourceScopedEndpoint attribute must exist");
        content.Should().Contain("class AdminEndpointAttribute", "AdminEndpoint attribute must exist");
    }

    [Fact]
    public void NoWriteAsJsonAnonymousError_InMiddleware()
    {
        var middlewareDir = Path.Combine(GetApiPath(), "Middleware");
        var files = Directory.GetFiles(middlewareDir, "*.cs")
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (content.Contains("WriteAsJsonAsync") && content.Contains("new { error"))
                violations.Add(Path.GetFileName(file));
        }

        violations.Should().BeEmpty($"Middleware must not write anonymous error objects via WriteAsJsonAsync. Use ProblemDetailsWriter instead. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void WorkspaceRoute_NoGuidEmptyFallback()
    {
        var endpointFiles = GetEndpointFiles();
        var violations = new List<string>();

        foreach (var file in endpointFiles)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (content.Contains("Guid.Empty") && content.Contains("workspaceId"))
                violations.Add(Path.GetFileName(file));
        }

        violations.Should().BeEmpty($"Endpoint files must not use Guid.Empty as workspaceId fallback: {string.Join(", ", violations)}");
    }

    [Fact]
    public void EndpointDirectories_ShouldHaveMatchingContractDirectories()
    {
        var apiPath = GetApiPath();

        var endpointsDir = Path.Combine(apiPath, "Endpoints");
        var endpointBcs = Directory.GetDirectories(endpointsDir)
            .Select(Path.GetFileName)
            .Where(d => d != "Health")
            .OrderBy(d => d)
            .ToArray();

        var contractsDir = Path.Combine(apiPath, "Contracts");
        var contractBcs = Directory.GetDirectories(contractsDir)
            .Select(Path.GetFileName)
            .Where(d => d != "Common" && d != "openapi")
            .OrderBy(d => d)
            .ToArray();

        endpointBcs.Should().BeEquivalentTo(contractBcs,
            "Every bounded context with endpoints should have a Contracts directory");
    }

    [Fact]
    public void EndpointHandlerFiles_MustNotCallRawAuth()
    {
        var files = GetEndpointFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (fileName == "EndpointMappingExtensions.cs") continue;

            // Only check *Endpoint.cs (handler files), not Map*Endpoints.cs (group files)
            if (!fileName.EndsWith("Endpoint.cs") || fileName.StartsWith("Map")) continue;

            var content = RemoveComments(File.ReadAllText(file));

            // Check for direct .RequireAuthorization or .AllowAnonymous calls
            if (content.Contains(".RequireAuthorization(") || content.Contains(".AllowAnonymous()"))
            {
                violations.Add(fileName);
            }
        }

        violations.Should().BeEmpty(
            $"Endpoint handler files must use Map{{Scope}}{{Verb}} DSL instead of raw .RequireAuthorization() or .AllowAnonymous(): " +
            $"{string.Join(", ", violations)}");
    }

    [Fact]
    public void MapFiles_MustNotUseTagAsScopeMetadata()
    {
        var apiPath = GetApiPath();
        var mapFiles = Directory.GetFiles(Path.Combine(apiPath, "Endpoints"), "Map*Endpoints.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
        var violations = new List<string>();

        foreach (var file in mapFiles)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var lines = content.Split('\n');

            foreach (var line in lines)
            {
                if (line.Contains(".TagAsPublic(") ||
                    line.Contains(".TagAsWorkspaceScoped(") ||
                    line.Contains(".TagAsResourceScoped(") ||
                    line.Contains(".TagAsAdmin("))
                {
                    violations.Add($"{Path.GetFileName(file)}: {line.Trim()}");
                }
            }
        }

        violations.Should().BeEmpty(
            $"TagAs* calls are removed; endpoint scoping now uses Map{{Scope}}{{Verb}} DSL: {string.Join(", ", violations)}");
    }

    private static string RemoveComments(string input)
    {
        var blockComments = @"/\*(.*?)\*/";
        var lineComments = @"//(.*?)\r?\n";
        var cleaned = Regex.Replace(input, blockComments, "", RegexOptions.Singleline);
        cleaned = Regex.Replace(cleaned, lineComments, "\n");
        return cleaned;
    }
}
