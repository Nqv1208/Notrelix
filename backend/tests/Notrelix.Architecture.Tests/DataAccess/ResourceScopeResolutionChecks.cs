namespace Notrelix.Architecture.Tests;

public class ResourceScopeResolutionChecks : ArchitectureTestBase
{
    private static string ResolverPath =>
        Path.Combine(GetInfrastructurePath(), "Services", "ResourceLocator.cs");

    [Fact]
    public void ResolverCoversAllResourceKindsUsedByEndpoints()
    {
        var resolverContent = File.ReadAllText(ResolverPath);

        var mapFiles = Directory.GetFiles(Path.Combine(GetApiPath(), "Endpoints"), "Map*Endpoints.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var kindsFromEndpoints = new HashSet<string>(StringComparer.Ordinal);

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
                var kindMatches = System.Text.RegularExpressions.Regex.Matches(
                    handlerContent, @"ResourceKind\.Create\(""([^""]+)""\)");
                foreach (System.Text.RegularExpressions.Match kindMatch in kindMatches)
                {
                    if (kindMatch.Success)
                        kindsFromEndpoints.Add(kindMatch.Groups[1].Value);
                }
            }
        }

        foreach (var kind in kindsFromEndpoints)
        {
            resolverContent.Should().Contain($"\"{kind}\"",
                $"ResourceLocator must handle the resource kind '{kind}' used by MapResource* endpoints");
        }
    }

    // Not endpoint-driven: a request the pipeline scopes to a resource whose kind the locator cannot resolve
    // is answered NotFound for everyone before its handler runs.
    [Fact]
    public void ResolverHandlesEveryResourceKindOfEveryResourceScopedRequest()
    {
        var resolverContent = File.ReadAllText(ResolverPath);
        var missing = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var file in Directory.GetFiles(Path.Combine(GetApplicationPath(), "Features"), "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                              && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (!content.Contains("IResourceScopedRequest", StringComparison.Ordinal)) continue;

            foreach (System.Text.RegularExpressions.Match kind in System.Text.RegularExpressions.Regex.Matches(
                         content, @"ResourceKind\.Create\(""([^""]+)""\)"))
            {
                if (!resolverContent.Contains($"\"{kind.Groups[1].Value}\" =>", StringComparison.Ordinal))
                    missing.Add(kind.Groups[1].Value);
            }
        }

        // Named exceptions: the kinds docs/product/governance.md lists as "Not yet wired". Wiring one means
        // removing it here and from that doc line; a new unhandled kind is never added to this set silently.
        var unwired = new SortedSet<string>(StringComparer.Ordinal)
        {
            "work-management.board-relation", "work-management.form", "work-management.form-submission",
        };

        missing.Should().BeEquivalentTo(unwired,
            "ResourceLocator.QueryAsync must have a case for every resource kind a scoped request uses, except the documented unwired kinds");

        var doc = File.ReadAllText(Path.Combine(GetSrcPath(), "..", "..", "docs", "product", "governance.md"));
        var line = doc.Split('\n').Single(l => l.StartsWith("Not yet wired", StringComparison.Ordinal));
        line.Should().Contain("Forms and form submissions").And.Contain("Board relations")
            .And.NotContain("approval", "approval requests are wired into the locator")
            .And.NotContain("Saved filters", "saved filters are wired into the locator");
    }

    [Fact]
    public void ResolverHandlesKnownResourceKinds()
    {
        var content = File.ReadAllText(ResolverPath);
        content.Should().Contain("\"work-management.label\"", "ResourceLocator must handle work-management.label");
        content.Should().Contain("\"governance.share-link\"", "ResourceLocator must handle governance.share-link");
        content.Should().Contain("\"work-management.checklist-item\"", "ResourceLocator must handle work-management.checklist-item");
        content.Should().Contain("\"governance.resource-permission\"", "ResourceLocator must handle governance.resource-permission");
        content.Should().Contain("\"automation.rule\"", "ResourceLocator must handle automation.rule");
        content.Should().Contain("\"automation.execution\"", "ResourceLocator must handle automation.execution");
    }
}
