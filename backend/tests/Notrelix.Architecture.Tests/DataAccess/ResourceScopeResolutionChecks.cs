namespace Notrelix.Architecture.Tests;

public class ResourceScopeResolutionChecks : ArchitectureTestBase
{
    [Fact]
    public void ResolverCoversAllResourceTypesUsedByEndpoints()
    {
        var resolverContent = File.ReadAllText(Path.Combine(GetInfrastructurePath(), "Services", "ResourceScopeResolver.cs"));

        var mapFiles = Directory.GetFiles(Path.Combine(GetApiPath(), "Endpoints"), "Map*Endpoints.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var resourceTypesFromEndpoints = new HashSet<string>();

        foreach (var mapFile in mapFiles)
        {
            var content = File.ReadAllText(mapFile);
            var matches = System.Text.RegularExpressions.Regex.Matches(content, @"<(\w+(Command|Query))>");
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (!match.Success) continue;
                var requestType = match.Groups[1].Value;

                var handlerFile = Directory.GetFiles(
                    Path.Combine(GetApplicationPath(), "Features"),
                    requestType + ".cs",
                    SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (handlerFile == null) continue;

                var handlerContent = RemoveComments(File.ReadAllText(handlerFile));
                var typeMatches = System.Text.RegularExpressions.Regex.Matches(handlerContent, @"ResourceType\.(\w+)");
                foreach (System.Text.RegularExpressions.Match typeMatch in typeMatches)
                {
                    if (typeMatch.Success)
                        resourceTypesFromEndpoints.Add(typeMatch.Groups[1].Value);
                }
            }
        }

        foreach (var rt in resourceTypesFromEndpoints)
        {
            resolverContent.Should().Contain($"ResourceType.{rt}",
                $"ResourceScopeResolver must handle ResourceType.{rt} used by MapResource* endpoints");
        }
    }

    [Fact]
    public void ResolverHandlesKnownResourceTypes()
    {
        var content = File.ReadAllText(Path.Combine(GetApplicationPath(), "..", "Notrelix.Infrastructure", "Services", "ResourceScopeResolver.cs"));
        content.Should().Contain("ResourceType.Label", "ResourceScopeResolver must handle Label");
        content.Should().Contain("ResourceType.ShareLink", "ResourceScopeResolver must handle ShareLink");
        content.Should().Contain("ResourceType.ChecklistItem", "ResourceScopeResolver must handle ChecklistItem");
        content.Should().Contain("ResourceType.ResourcePermission", "ResourceScopeResolver must handle ResourcePermission");
        content.Should().Contain("ResourceType.AutomationRule", "ResourceScopeResolver must handle AutomationRule");
        content.Should().Contain("ResourceType.AutomationExecution", "ResourceScopeResolver must handle AutomationExecution");
    }
}
