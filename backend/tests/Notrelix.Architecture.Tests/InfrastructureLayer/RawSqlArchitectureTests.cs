namespace Notrelix.Architecture.Tests.InfrastructureLayer;

public class RawSqlArchitectureTests
{
    private static readonly HashSet<string> AllowedRawSqlUsages = new(StringComparer.OrdinalIgnoreCase)
    {
        // Reason: applies PostgreSQL RLS session variables
        "src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs",

        // Reason: sets READ ONLY transaction mode for read-scoped requests
        "src/Notrelix.Infrastructure/Data/EfRequestDataSession.cs",

        // Reason: atomic first-use seed for workspace feature usage with
        // INSERT ... SELECT ... ON CONFLICT (relies on PostgreSQL unique-scope
        // semantics that EF cannot express)
        "src/Notrelix.Infrastructure/Data/ApplicationDbContext.BillingCapacity.cs",

        // Reason: claim outbox messages with SKIP LOCKED
        "src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs",
        
        // Reason: claim outbox messages with SKIP LOCKED
        "src/Notrelix.Infrastructure/BackgroundJobs/EmailDispatcher.cs",
        
        // Reason: query permission version with MAX(updated_at)
        "src/Notrelix.Infrastructure/Governance/Services/PermissionVersionProvider.cs",

        // Reason: lock active verification tokens with FromSqlInterpolated
        "src/Notrelix.Infrastructure/Identity/Services/ActiveVerificationTokenLocker.cs",

        // Reason: serialize owner-affecting workspace membership ops with
        // SELECT ... FOR UPDATE on the workspace row (PostgreSQL row lock)
        "src/Notrelix.Infrastructure/Workspaces/Members/WorkspaceOwnerUpdateLocker.cs",
        // WX-BE-001/002: transaction-scoped advisory lock (pg_advisory_xact_lock)
        // serializing one member's navigation writes (recents trim, pin cap)
        // inside the request transaction.
        "src/Notrelix.Infrastructure/Workspaces/Navigation/WorkspaceNavigationStore.cs",
        // WM-V3-ORDER-001: transaction-scoped advisory lock (pg_advisory_xact_lock)
        // serializing one Work ordering scope inside the request transaction.
        "src/Notrelix.Infrastructure/Data/WorkManagement/PostgresWorkOrderingScopeLock.cs",
        // Transaction-scoped advisory lock (pg_advisory_xact_lock) serializing
        // one Board's Item-key allocation inside the request transaction.
        "src/Notrelix.Infrastructure/Data/WorkManagement/PostgresBoardItemKeyLock.cs",
        // ADR-009: transaction-scoped advisory lock (pg_advisory_xact_lock)
        // serializing one Workspace's Space hierarchy writes (create/move/rename/
        // restore) so ancestry, sibling and closure reads cannot interleave.
        "src/Notrelix.Infrastructure/Workspaces/Spaces/SpaceStore.cs",
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
