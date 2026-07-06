namespace Notrelix.Architecture.Tests;

public class ResourceEndpointContractArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void MapResourceEndpoints_SendIResourceScopedRequest()
    {
        var mapFiles = Directory.GetFiles(Path.Combine(GetApiPath(), "Endpoints"), "Map*Endpoints.cs", SearchOption.AllDirectories)
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
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("MapResourceGet") && !trimmed.StartsWith("MapResourcePost")
                    && !trimmed.StartsWith("MapResourcePut") && !trimmed.StartsWith("MapResourcePatch")
                    && !trimmed.StartsWith("MapResourceDelete"))
                    continue;

                var handlerMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"<([A-Za-z]+(Command|Query))>");
                if (handlerMatch.Success)
                {
                    var requestType = handlerMatch.Groups[1].Value;
                    var handlerFile = Directory.GetFiles(
                        Path.Combine(GetApplicationPath(), "Features"),
                        requestType + ".cs",
                        SearchOption.AllDirectories)
                        .FirstOrDefault();
                    if (handlerFile != null)
                    {
                        var handlerContent = RemoveComments(File.ReadAllText(handlerFile));
                        if (!handlerContent.Contains("IResourceScopedRequest"))
                            violations.Add($"{Path.GetFileName(file)}: {trimmed} sends {requestType} which is not IResourceScopedRequest");
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "Every MapResource* endpoint must send a request that implements IResourceScopedRequest. " +
            "Violations: " + string.Join(", ", violations));
    }

    [Fact]
    public void EndpointHandlers_DoNotUseXWorkspaceId()
    {
        var resourceScopedEndpointFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var mapFiles = Directory.GetFiles(Path.Combine(GetApiPath(), "Endpoints"), "Map*Endpoints.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

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
                resourceScopedEndpointFiles.Add(Path.GetFileName(hf));
        }

        var violations = new List<string>();

        foreach (var file in Directory.GetFiles(Path.Combine(GetApiPath(), "Endpoints"), "*Endpoint.cs", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(file);
            if (!resourceScopedEndpointFiles.Contains(fileName)) continue;

            var content = RemoveComments(File.ReadAllText(file));
            if (content.Contains("X-Workspace-Id") || content.Contains("TryGetWorkspaceIdHint") || content.Contains("TryGetValue(\"X-Workspace-Id\""))
                violations.Add(fileName);
        }

        violations.Should().BeEmpty(
            "MapResource* endpoint handlers must not reference X-Workspace-Id or TryGetWorkspaceIdHint. " +
            "Violations: " + string.Join(", ", violations));
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
}
