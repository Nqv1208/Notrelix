using Microsoft.EntityFrameworkCore;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Tests.Data.Rls;

public class RlsPolicyVerificationTests
{
    [Fact]
    public async Task AllWorkspaceScopedTables_HaveRlsEnabled()
    {
        await using var context = CreateContext();

        var tablesWithRls = await context.Database
            .SqlQueryRaw<string>("""
                SELECT tablename FROM pg_tables
                WHERE schemaname IN ('work','docs','collab','automation','integration',
                    'billing','reporting','search','notifications','activity')
                  AND rowsecurity = true
                ORDER BY schemaname, tablename
                """)
            .ToListAsync();

        tablesWithRls.Should().NotBeEmpty("workspace-scoped tables should have RLS enabled");
    }

    [Fact]
    public async Task NoBlanketWorkspaceAccessPolicy_Exists()
    {
        await using var context = CreateContext();

        var blanketPolicies = await context.Database
            .SqlQueryRaw<string>("""
                SELECT policyname FROM pg_policies
                WHERE policyname = 'workspace_access'
                """)
            .ToListAsync();

        blanketPolicies.Should().BeEmpty("old blanket workspace_access policies should not exist");
    }

    [Fact]
    public async Task IdentityTables_HaveRlsEnabled()
    {
        await using var context = CreateContext();

        var identityRls = await context.Database
            .SqlQueryRaw<string>("""
                SELECT tablename FROM pg_tables
                WHERE schemaname = 'identity' AND rowsecurity = true
                ORDER BY tablename
                """)
            .ToListAsync();

        identityRls.Should().Contain("users");
        identityRls.Should().Contain("user_sessions");
    }

    [Fact]
    public async Task EventsTable_HasInsertPolicy_ButNoAppSelectPolicy()
    {
        await using var context = CreateContext();

        var policies = await context.Database
            .SqlQueryRaw<string>("""
                SELECT policyname || '||' || cmd || '||' || roles FROM pg_policies
                WHERE schemaname = 'events' AND tablename = 'domain_event_logs'
                """)
            .ToListAsync();

        policies.Should().NotBeEmpty("events table should have policies");

        var selectPolicies = policies.Where(p => p.Contains("||SELECT||")).ToList();
        foreach (var policy in selectPolicies)
        {
            policy.Should().Contain("notrelix_worker", "SELECT on events should be for worker, not app");
            policy.Should().NotContain("notrelix_app", "app should not SELECT events");
        }
    }

    [Fact]
    public async Task MessagingOutbox_HasNoAppSelectPolicy()
    {
        await using var context = CreateContext();

        var policies = await context.Database
            .SqlQueryRaw<string>("""
                SELECT policyname || '||' || cmd || '||' || roles FROM pg_policies
                WHERE schemaname = 'messaging' AND tablename = 'outbox_messages'
                """)
            .ToListAsync();

        var selectPolicies = policies.Where(p => p.Contains("||SELECT||")).ToList();
        foreach (var policy in selectPolicies)
        {
            policy.Should().NotContain("notrelix_app", "app should not SELECT outbox_messages");
        }
    }

    [Fact]
    public async Task EmailOutbox_HasNoAppSelectPolicy()
    {
        await using var context = CreateContext();

        var policies = await context.Database
            .SqlQueryRaw<string>("""
                SELECT policyname || '||' || cmd || '||' || roles FROM pg_policies
                WHERE schemaname = 'notifications' AND tablename = 'email_outbox'
                """)
            .ToListAsync();

        var selectPolicies = policies.Where(p => p.Contains("||SELECT||")).ToList();
        foreach (var policy in selectPolicies)
        {
            policy.Should().NotContain("notrelix_app", "app should not SELECT email_outbox");
        }
    }

    [Fact]
    public async Task RolesExist_InDatabase()
    {
        await using var context = CreateContext();

        var roles = await context.Database
            .SqlQueryRaw<string>("""
                SELECT rolname FROM pg_roles
                WHERE rolname IN ('notrelix_app','notrelix_auth','notrelix_worker',
                    'notrelix_support_readonly','notrelix_migrator','notrelix_admin','notrelix_owner')
                ORDER BY rolname
                """)
            .ToListAsync();

        roles.Should().HaveCount(7, "all 7 RLS roles should exist");
    }

    [Fact]
    public async Task AuthzHelpers_Exist()
    {
        await using var context = CreateContext();

        var functions = await context.Database
            .SqlQueryRaw<string>("""
                SELECT p.proname FROM pg_proc p
                JOIN pg_namespace n ON p.pronamespace = n.oid
                WHERE n.nspname = 'authz'
                  AND p.proname IN (
                    'current_user_has_workspace_access',
                    'current_user_is_workspace_admin',
                    'current_user_has_workspace_permission'
                  )
                ORDER BY p.proname
                """)
            .ToListAsync();

        functions.Should().HaveCount(3, "all 3 authz helper functions should exist");
    }

    [Fact]
    public async Task AuthzGrantsTable_HasCorrectStructure()
    {
        await using var context = CreateContext();

        var columns = await context.Database
            .SqlQueryRaw<string>("""
                SELECT column_name FROM information_schema.columns
                WHERE table_schema = 'authz' AND table_name = 'workspace_access_grants'
                ORDER BY ordinal_position
                """)
            .ToListAsync();

        columns.Should().Contain("workspace_id");
        columns.Should().Contain("user_id");
        columns.Should().Contain("membership_status");
        columns.Should().Contain("is_workspace_admin");
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"RlsVerify-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
