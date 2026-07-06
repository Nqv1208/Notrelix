namespace Notrelix.Architecture.Tests;

public class Phase4ArchitectureTests
{
    private static string GetSrcPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return Path.Combine(current, "src");
    }

    private static string GetApplicationPath() => Path.Combine(GetSrcPath(), "Notrelix.Application");
    private static string GetApiPath() => Path.Combine(GetSrcPath(), "Notrelix.API");
    private static string GetDomainPath() => Path.Combine(GetSrcPath(), "Notrelix.Domain");
    private static string GetInfrastructurePath() => Path.Combine(GetSrcPath(), "Notrelix.Infrastructure");

    [Fact]
    public void ResourceType_IncludesLabel()
    {
        var content = File.ReadAllText(Path.Combine(GetDomainPath(), "SharedKernel", "ResourceType.cs"));
        content.Should().Contain("Label", "ResourceType enum must include Label for label resources");
    }

    [Fact]
    public void ResourceScopeResolver_HandlesNewResources()
    {
        var content = File.ReadAllText(Path.Combine(GetApplicationPath(), "..", "Notrelix.Infrastructure", "Services", "ResourceScopeResolver.cs"));
        content.Should().Contain("ResourceType.Label", "ResourceScopeResolver must handle Label");
        content.Should().Contain("ResourceType.ShareLink", "ResourceScopeResolver must handle ShareLink");
        content.Should().Contain("ResourceType.ChecklistItem", "ResourceScopeResolver must handle ChecklistItem");
        content.Should().Contain("ResourceType.ResourcePermission", "ResourceScopeResolver must handle ResourcePermission");
        content.Should().Contain("ResourceType.AutomationRule", "ResourceScopeResolver must handle AutomationRule");
        content.Should().Contain("ResourceType.AutomationExecution", "ResourceScopeResolver must handle AutomationExecution");
    }

    [Fact]
    public void PermissionContext_HasAccountId()
    {
        var content = File.ReadAllText(Path.Combine(GetApplicationPath(), "Common", "Security", "PermissionContext.cs"));
        content.Should().Contain("Guid AccountId", "PermissionContext record must have AccountId field");
    }

    [Fact]
    public void HealthEndpoint_IsInternal()
    {
        var content = File.ReadAllText(Path.Combine(GetApiPath(), "Endpoints", "Health", "HealthEndpoints.cs"));
        content.Should().Contain("MapInternalGet(\"/\", GetHealth)", "Health endpoint must use MapInternalGet for /health");
        content.Should().NotContain("MapPublicGet(\"/\", GetHealth)", "Health endpoint must not use MapPublicGet");
    }

    [Fact]
    public void GovernanceCommands_NoWorkspaceId()
    {
        var files = Directory.GetFiles(Path.Combine(GetApplicationPath(), "Features", "Governance"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (!fileName.EndsWith("Command.cs") && !fileName.EndsWith("Query.cs")) continue;

            var content = File.ReadAllText(file);
            if (!content.Contains("IResourceScopedRequest")) continue;

            content.Should().NotContain("Guid WorkspaceId,",
                $"{fileName} implements IResourceScopedRequest but still declares WorkspaceId parameter");
        }
    }

    [Fact]
    public void MapResourceEndpointHandlers_NoWorkspaceIdParam()
    {
        var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CreateShareLinkEndpoint.cs",
            "DisableShareLinkEndpoint.cs",
            "GetResourcePermissionsEndpoint.cs",
            "GrantResourcePermissionEndpoint.cs",
            "RevokeResourcePermissionEndpoint.cs",
            "ListWorkspaceBoardsEndpoint.cs",
            "CreateBoardEndpoint.cs",
            "GetBoardEndpoint.cs",
            "UpdateBoardEndpoint.cs",
            "ArchiveBoardEndpoint.cs",
            "UnarchiveBoardEndpoint.cs",
            "CreateBoardGroupEndpoint.cs",
            "UpdateBoardGroupEndpoint.cs",
            "ArchiveBoardGroupEndpoint.cs",
            "UnarchiveBoardGroupEndpoint.cs",
            "ReorderBoardGroupsEndpoint.cs",
            "ListBoardFieldsEndpoint.cs",
            "CreateBoardFieldEndpoint.cs",
            "UpdateBoardFieldEndpoint.cs",
            "ReorderBoardFieldsEndpoint.cs",
            "DeleteBoardFieldEndpoint.cs",
        };

        var endpointFiles = Directory.GetFiles(Path.Combine(GetApiPath(), "Endpoints"), "*Endpoint.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var mapFiles = Directory.GetFiles(Path.Combine(GetApiPath(), "Endpoints"), "Map*Endpoints.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var resourceScopedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapFile in mapFiles)
        {
            var content = File.ReadAllText(mapFile);
            if (!content.Contains("MapResourceGet") && !content.Contains("MapResourcePost")
                && !content.Contains("MapResourcePut") && !content.Contains("MapResourcePatch")
                && !content.Contains("MapResourceDelete"))
                continue;

            var mapDir = Path.GetDirectoryName(mapFile);
            if (mapDir == null) continue;

            var handlerFiles = Directory.GetFiles(mapDir, "*Endpoint.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                         && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                .ToArray();

            foreach (var hf in handlerFiles)
                resourceScopedFiles.Add(Path.GetFileName(hf));
        }

        var violations = new List<string>();

        foreach (var file in endpointFiles)
        {
            var fileName = Path.GetFileName(file);
            if (!resourceScopedFiles.Contains(fileName)) continue;
            if (allowedFiles.Contains(fileName)) continue;

            var content = RemoveComments(File.ReadAllText(file));
            if (content.Contains("Guid workspaceId") || content.Contains("Guid WorkspaceId"))
            {
                violations.Add(fileName);
            }
        }

        violations.Should().BeEmpty(
            $"MapResource* endpoint handlers must not have workspaceId parameter. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void NoUtcNowInApplicationHandlers()
    {
        var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "RefreshToken.cs",
            "ResetPassword.cs",
        };

        var handlerFiles = Directory.GetFiles(Path.Combine(GetApplicationPath(), "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => File.ReadAllText(f).Contains("IRequestHandler<"))
            .ToArray();

        var violations = new List<string>();

        foreach (var file in handlerFiles)
        {
            var fileName = Path.GetFileName(file);
            if (allowedFiles.Contains(fileName)) continue;

            var content = RemoveComments(File.ReadAllText(file));
            if (content.Contains("DateTime.UtcNow") || content.Contains("DateTimeOffset.UtcNow"))
            {
                violations.Add(fileName);
            }
        }

        violations.Should().BeEmpty(
            $"Handlers must use IDateTimeProvider instead of DateTime.UtcNow. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void AggregateRoot_NoPublicParameterlessConstructor()
    {
        var domainPath = GetDomainPath();
        var aggregateFiles = Directory.GetFiles(domainPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f =>
            {
                var content = File.ReadAllText(f);
                return content.Contains(": AggregateRoot") || content.Contains("AggregateRoot,");
            })
            .ToArray();

        var violations = new List<string>();

        foreach (var file in aggregateFiles)
        {
            if (file.EndsWith("AggregateRoot.cs") && file.Contains("Common")) continue;

            var fileName = Path.GetFileNameWithoutExtension(file);
            var content = RemoveComments(File.ReadAllText(file));

            if (content.Contains("public " + fileName + "()") && !content.Contains("abstract class"))
            {
                violations.Add(Path.GetFileName(file));
            }
        }

        violations.Should().BeEmpty(
            $"Aggregate roots must not have public parameterless constructors. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void ValueObject_ExtendsValueObjectBase()
    {
        var domainPath = GetDomainPath();
        var valueObjectFiles = Directory.GetFiles(domainPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f =>
            {
                var content = File.ReadAllText(f);
                return content.Contains(": ValueObject") || content.Contains("ValueObject,");
            })
            .ToArray();

        valueObjectFiles.Should().NotBeEmpty("ValueObject files must exist in Domain");

        foreach (var file in valueObjectFiles)
        {
            if (file.EndsWith("ValueObject.cs") && file.Contains($"{Path.DirectorySeparatorChar}Common{Path.DirectorySeparatorChar}")) continue;

            var content = File.ReadAllText(file);
            var extendsValueObject = content.Contains(": ValueObject") || content.Contains("ValueObject,");

            extendsValueObject.Should().BeTrue(
                $"Value object file '{Path.GetFileName(file)}' must extend ValueObject base class");
        }
    }

    [Fact]
    public void Infrastructure_DoesNotImportDomainEventNamespaces()
    {
        var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DomainEventDispatchPolicy.cs",
            "ApplicationDbContext.DbSets.cs",
            "InboundWebhookEventConfiguration.cs",
            "BillingEventConfiguration.cs",
        };

        var allowedUsings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "using Notrelix.Domain.SharedKernel;",
        };

        var files = Directory.GetFiles(GetInfrastructurePath(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (allowedFiles.Contains(Path.GetFileName(file))) continue;

            var lines = content.Split('\n');

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("using Notrelix.Domain")) continue;
                if (allowedUsings.Contains(trimmed.TrimEnd(';'))) continue;

                if (trimmed.Contains("Events") || trimmed.Contains("DomainEvent"))
                {
                    violations.Add($"{Path.GetFileName(file)}: {trimmed}");
                }
            }
        }

        violations.Should().BeEmpty(
            $"Infrastructure must not directly import Domain event namespaces. " +
            $"Violations: {string.Join(", ", violations)}");
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
