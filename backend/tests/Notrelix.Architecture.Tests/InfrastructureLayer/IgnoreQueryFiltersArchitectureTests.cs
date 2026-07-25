namespace Notrelix.Architecture.Tests.InfrastructureLayer;

public class IgnoreQueryFiltersArchitectureTests
{
    private static readonly HashSet<string> AllowedIgnoreQueryFiltersUsages = new(StringComparer.OrdinalIgnoreCase)
    {
        "src/Notrelix.Infrastructure/Data/ApplicationDbContextInitialiser.cs",
        "src/Notrelix.Infrastructure/Services/TenantBootstrapStore.cs",
        "src/Notrelix.Infrastructure/Services/WorkspaceAccessResolver.cs",
        "src/Notrelix.Infrastructure/Services/ResourceScopeResolver.cs",
        "src/Notrelix.Infrastructure/Notifications/Email/EmailTemplateMaterialization.cs",
        "src/Notrelix.Application/Features/Identity/Verification/Services/EmailVerificationTokenIssuer.cs",
    };

    [Fact]
    public void IgnoreQueryFilters_MustOnlyBeUsedInAllowlist()
    {
        var infrastructurePath = Path.Combine(FindProjectRoot(), "src", "Notrelix.Infrastructure");
        var files = Directory.GetFiles(infrastructurePath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(FindProjectRoot(), file).Replace('\\', '/');

            if (content.Contains("IgnoreQueryFilters()") &&
                !AllowedIgnoreQueryFiltersUsages.Contains(relativePath))
            {
                violations.Add(relativePath);
            }
        }

        violations.Should().BeEmpty(
            $"IgnoreQueryFilters() must only be used in allowlisted infrastructure files. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    private static string FindProjectRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null && !File.Exists(Path.Combine(dir, "backend.slnx")))
            dir = Directory.GetParent(dir)?.FullName;
        return dir ?? throw new InvalidOperationException("Could not find project root");
    }
}
