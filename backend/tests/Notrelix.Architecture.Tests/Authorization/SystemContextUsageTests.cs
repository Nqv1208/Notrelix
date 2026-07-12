namespace Notrelix.Architecture.Tests;

public class SystemContextUsageTests
{
    private static string GetSourcePath()
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

    /// <summary>
    /// Files that are intentionally allowed to use IgnoreQueryFilters().
    /// Seed/cleanup code, tenant access resolver, and restore operations.
    /// </summary>
    private static readonly HashSet<string> IgnoreQueryFiltersAllowlist =
    [
        "ApplicationDbContextInitialiser.cs",
        "WorkspaceAccessResolver.cs",
        "RestoreWorkspace.cs",
        "TenantBootstrapStore.cs",
        "ResourceScopeResolver.cs",
        "EmailTemplateMaterialization.cs",
        "EmailVerificationTokenIssuer.cs",
    ];

    [Fact]
    public void IgnoreQueryFilters_ShouldOnlyAppearInAllowlistedFiles()
    {
        var srcPath = GetSourcePath();
        var violations = new List<string>();

        foreach (var file in Directory.GetFiles(srcPath, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                continue;

            var content = File.ReadAllText(file);
            if (!content.Contains(".IgnoreQueryFilters()"))
                continue;

            var fileName = Path.GetFileName(file);
            if (IgnoreQueryFiltersAllowlist.Contains(fileName))
                continue;

            violations.Add(fileName);
        }

        violations.Should().BeEmpty(
            $"IgnoreQueryFilters() must only appear in allowlisted files. " +
            $"Violations: {string.Join(", ", violations)}");
    }
}
