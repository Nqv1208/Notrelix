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

        // Reason: the access grant is written and revoked through the owner-defined ops.upsert_access_grant /
        // ops.revoke_access_grant functions (the application role has no grant write policy)
        "src/Notrelix.Infrastructure/Data/Authz/AccessGrantProjectionService.cs",

        // Reason: the tenantless and tenant security event row is written through the owner-defined
        // insert-only ops.record_security_event function
        "src/Notrelix.Infrastructure/Data/Audit/SecurityAuditSink.cs",

        // Reason: claim outbox messages with SKIP LOCKED
        "src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs",

        // Reason: delete Board access-change rows past retention_until in bounded batches, claimed with
        // FOR UPDATE SKIP LOCKED so several instances can sweep at once (off by default)
        "src/Notrelix.Infrastructure/BackgroundJobs/ResourceAccessChangeRetention.cs",
        
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
        // Attachment cap: transaction-scoped advisory lock (pg_advisory_xact_lock)
        // taken only at the commit point of an attachment upload/link, serializing
        // the per-item cap recount with the row insert inside the request transaction.
        "src/Notrelix.Infrastructure/Data/Collaboration/PostgresAttachmentTargetLock.cs",
        // Attachment object reaper: FOR UPDATE SKIP LOCKED claim of purge-due
        // soft-deleted rows and a try-advisory sweep claim, under the system context.
        "src/Notrelix.Infrastructure/BackgroundJobs/PostgresAttachmentReaperStore.cs",
        // Avatar object reaper: try-advisory sweep claim under the system context.
        "src/Notrelix.Infrastructure/BackgroundJobs/PostgresAvatarReaperStore.cs",
        // Security activity retention: bounded DELETE ... FOR UPDATE SKIP LOCKED sweep of
        // identity.user_security_activity under the system context.
        "src/Notrelix.Infrastructure/BackgroundJobs/SecurityActivityRetention.cs",
        // Onboarding draft sweep: bounded DELETE ... FOR UPDATE SKIP LOCKED of expired
        // workspace.onboarding_drafts rows under the system context.
        "src/Notrelix.Infrastructure/BackgroundJobs/OnboardingDraftSweep.cs",
        // Reserves a Board's Item keys with one row-locking UPDATE ... RETURNING
        // on work.boards inside the request transaction (EF cannot express it
        // without loading and versioning the Board aggregate).
        "src/Notrelix.Infrastructure/Data/WorkManagement/PostgresBoardItemKeyAllocator.cs",
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
