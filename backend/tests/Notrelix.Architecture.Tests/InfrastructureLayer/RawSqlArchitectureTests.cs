namespace Notrelix.Architecture.Tests.InfrastructureLayer;

public class RawSqlArchitectureTests
{
    private static readonly HashSet<string> AllowedRawSqlUsages = new(StringComparer.OrdinalIgnoreCase)
    {
        "src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs",
        "src/Notrelix.Infrastructure/BackgroundJobs/EmailDispatcher.cs",
        "src/Notrelix.Infrastructure/Governance/Services/PermissionVersionProvider.cs",
        "src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs",
    };

    [Fact]
    public void FromSqlRaw_MustOnlyBeUsedInAllowlist()
    {
        var infrastructurePath = Path.Combine(FindProjectRoot(), "src", "Notrelix.Infrastructure");
        var files = Directory.GetFiles(infrastructurePath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(FindProjectRoot(), file).Replace('\\', '/');

            if (content.Contains("FromSqlRaw(") &&
                !AllowedRawSqlUsages.Contains(relativePath))
            {
                violations.Add(relativePath);
            }
        }

        violations.Should().BeEmpty(
            $"FromSqlRaw() must only be used in allowlisted infrastructure files. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void ExecuteSqlRaw_MustOnlyBeUsedInAllowlist()
    {
        var infrastructurePath = Path.Combine(FindProjectRoot(), "src", "Notrelix.Infrastructure");
        var files = Directory.GetFiles(infrastructurePath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(FindProjectRoot(), file).Replace('\\', '/');

            if (content.Contains("ExecuteSqlRaw(") &&
                !AllowedRawSqlUsages.Contains(relativePath))
            {
                violations.Add(relativePath);
            }
        }

        violations.Should().BeEmpty(
            $"ExecuteSqlRaw() must only be used in allowlisted infrastructure files. " +
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
