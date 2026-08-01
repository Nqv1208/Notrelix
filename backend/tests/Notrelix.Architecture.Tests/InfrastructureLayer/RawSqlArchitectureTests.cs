namespace Notrelix.Architecture.Tests.InfrastructureLayer;

public class RawSqlArchitectureTests
{
    private static readonly HashSet<string> AllowedRawSqlUsages = new(StringComparer.OrdinalIgnoreCase)
    {
        // Reason: applies PostgreSQL RLS session variables
        "src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs",

        // Reason: sets READ ONLY transaction mode for read-scoped requests
        "src/Notrelix.Infrastructure/Data/EfRequestDataSession.cs",

        // Reason: claim outbox messages with SKIP LOCKED
        "src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs",
        
        // Reason: claim outbox messages with SKIP LOCKED
        "src/Notrelix.Infrastructure/BackgroundJobs/EmailDispatcher.cs",
        
        // Reason: query permission version with MAX(updated_at)
        "src/Notrelix.Infrastructure/Governance/Services/PermissionVersionProvider.cs",
    };

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

            // Check cả sync và async variants
            if ((content.Contains("ExecuteSqlRaw(") || content.Contains("ExecuteSqlRawAsync(")) &&
                !AllowedRawSqlUsages.Contains(relativePath))
            {
                violations.Add(relativePath);
            }
        }

        violations.Should().BeEmpty(
            $"ExecuteSqlRaw/ExecuteSqlRawAsync must only be used in allowlisted infrastructure files. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void ExecuteSqlInterpolated_MustOnlyBeUsedInAllowlist()
    {
        var infrastructurePath = Path.Combine(FindProjectRoot(), "src", "Notrelix.Infrastructure");
        var files = Directory.GetFiles(infrastructurePath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(FindProjectRoot(), file).Replace('\\', '/');

            // Check cả sync và async variants
            if ((content.Contains("ExecuteSqlInterpolated(") || content.Contains("ExecuteSqlInterpolatedAsync(")) &&
                !AllowedRawSqlUsages.Contains(relativePath))
            {
                violations.Add(relativePath);
            }
        }

        violations.Should().BeEmpty(
            $"ExecuteSqlInterpolated/ExecuteSqlInterpolatedAsync must only be used in allowlisted infrastructure files. " +
            $"Violations: {string.Join(", ", violations)}");
    }

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
    public void FromSqlInterpolated_MustOnlyBeUsedInAllowlist()
    {
        var infrastructurePath = Path.Combine(FindProjectRoot(), "src", "Notrelix.Infrastructure");
        var files = Directory.GetFiles(infrastructurePath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(FindProjectRoot(), file).Replace('\\', '/');

            if (content.Contains("FromSqlInterpolated(") &&
                !AllowedRawSqlUsages.Contains(relativePath))
            {
                violations.Add(relativePath);
            }
        }

        violations.Should().BeEmpty(
            $"FromSqlInterpolated() must only be used in allowlisted infrastructure files. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void SqlQueryRaw_MustOnlyBeUsedInAllowlist()
    {
        var infrastructurePath = Path.Combine(FindProjectRoot(), "src", "Notrelix.Infrastructure");
        var files = Directory.GetFiles(infrastructurePath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(FindProjectRoot(), file).Replace('\\', '/');

            if (content.Contains("SqlQueryRaw(") &&
                !AllowedRawSqlUsages.Contains(relativePath))
            {
                violations.Add(relativePath);
            }
        }

        violations.Should().BeEmpty(
            $"SqlQueryRaw() must only be used in allowlisted infrastructure files. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void SqlQueryInterpolated_MustOnlyBeUsedInAllowlist()
    {
        var infrastructurePath = Path.Combine(FindProjectRoot(), "src", "Notrelix.Infrastructure");
        var files = Directory.GetFiles(infrastructurePath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(FindProjectRoot(), file).Replace('\\', '/');

            if (content.Contains("SqlQueryInterpolated(") &&
                !AllowedRawSqlUsages.Contains(relativePath))
            {
                violations.Add(relativePath);
            }
        }

        violations.Should().BeEmpty(
            $"SqlQueryInterpolated() must only be used in allowlisted infrastructure files. " +
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
