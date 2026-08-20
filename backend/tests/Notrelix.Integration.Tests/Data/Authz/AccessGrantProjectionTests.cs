using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Data.Authz;

[Collection("Database")]
public class AccessGrantProjectionTests : IAsyncLifetime
{
    private static readonly DateTimeOffset FixedTime = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public AccessGrantProjectionTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    [Fact]
    public async Task SyncAccountMemberGrant_WhenNoGrantExists_CreatesAccountLevelGrant()
    {
        await using var context = _db.CreateContext(SystemTenant());
        var projection = new AccessGrantProjectionService(context);
        var accountId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        await projection.SyncAccountMemberGrantAsync(accountId, userId, AccountRole.Owner, FixedTime, CancellationToken.None);
        await context.SaveChangesAsync();

        var grant = await context.AccessGrants.SingleAsync(g => g.AccountId == accountId && g.UserId == userId);
        grant.WorkspaceId.Should().BeNull();
        grant.SourceContext.Should().Be("Account");
        grant.MembershipStatus.Should().Be("Active");
        grant.RoleCodes.Should().BeEquivalentTo(["Owner"]);
        grant.IsAccountAdmin.Should().BeTrue("Owner maps to the account-admin RLS flag");
        grant.IsWorkspaceAdmin.Should().BeFalse();
        grant.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task SyncAccountMemberGrant_WhenGrantExists_UpdatesRoleAndAdminFlag()
    {
        await using var context = _db.CreateContext(SystemTenant());
        var projection = new AccessGrantProjectionService(context);
        var accountId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        await projection.SyncAccountMemberGrantAsync(accountId, userId, AccountRole.Owner, FixedTime, CancellationToken.None);
        await context.SaveChangesAsync();

        await projection.SyncAccountMemberGrantAsync(accountId, userId, AccountRole.Member, FixedTime.AddHours(1), CancellationToken.None);
        await context.SaveChangesAsync();

        var grants = await context.AccessGrants.Where(g => g.AccountId == accountId && g.UserId == userId).ToListAsync();
        grants.Should().HaveCount(1, "the account-level grant is updated in place, never duplicated");
        grants[0].RoleCodes.Should().BeEquivalentTo(["Member"]);
        grants[0].IsAccountAdmin.Should().BeFalse("Member is not an account admin");
        grants[0].UpdatedAt.Should().Be(FixedTime.AddHours(1));
    }

    [Fact]
    public async Task SyncWorkspaceMemberGrant_WhenNoGrantExists_CreatesWorkspaceGrant()
    {
        await using var context = _db.CreateContext(SystemTenant());
        var projection = new AccessGrantProjectionService(context);
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        await projection.SyncWorkspaceMemberGrantAsync(accountId, workspaceId, userId, WorkspaceRole.Admin, FixedTime, CancellationToken.None);
        await context.SaveChangesAsync();

        var grant = await context.AccessGrants.SingleAsync(
            g => g.AccountId == accountId && g.WorkspaceId == workspaceId && g.UserId == userId);
        grant.SourceContext.Should().Be("Workspace");
        grant.MembershipStatus.Should().Be("Active");
        grant.RoleCodes.Should().BeEquivalentTo(["Admin"]);
        grant.IsWorkspaceAdmin.Should().BeTrue();
        grant.IsAccountAdmin.Should().BeFalse("a workspace grant never elevates account scope");
    }

    [Fact]
    public async Task RevokeWorkspaceMemberGrant_WhenGrantExists_SetsRevokedAt()
    {
        await using var context = _db.CreateContext(SystemTenant());
        var projection = new AccessGrantProjectionService(context);
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        await projection.SyncWorkspaceMemberGrantAsync(accountId, workspaceId, userId, WorkspaceRole.Member, FixedTime, CancellationToken.None);
        await context.SaveChangesAsync();

        await projection.RevokeWorkspaceMemberGrantAsync(accountId, workspaceId, userId, FixedTime.AddDays(1), CancellationToken.None);
        await context.SaveChangesAsync();

        var grant = await context.AccessGrants.SingleAsync(
            g => g.AccountId == accountId && g.WorkspaceId == workspaceId && g.UserId == userId);
        grant.RevokedAt.Should().Be(FixedTime.AddDays(1));
    }

    [Fact]
    public async Task RevokeWorkspaceMemberGrant_WhenNoGrantExists_DoesNothing()
    {
        await using var context = _db.CreateContext(SystemTenant());
        var projection = new AccessGrantProjectionService(context);

        var act = () => projection.RevokeWorkspaceMemberGrantAsync(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), FixedTime, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncWorkspaceMemberGrant_AfterRevoke_ReactivatesGrant()
    {
        await using var context = _db.CreateContext(SystemTenant());
        var projection = new AccessGrantProjectionService(context);
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        await projection.SyncWorkspaceMemberGrantAsync(accountId, workspaceId, userId, WorkspaceRole.Member, FixedTime, CancellationToken.None);
        await context.SaveChangesAsync();
        await projection.RevokeWorkspaceMemberGrantAsync(accountId, workspaceId, userId, FixedTime.AddDays(1), CancellationToken.None);
        await context.SaveChangesAsync();

        await projection.SyncWorkspaceMemberGrantAsync(accountId, workspaceId, userId, WorkspaceRole.Admin, FixedTime.AddDays(2), CancellationToken.None);
        await context.SaveChangesAsync();

        var grant = await context.AccessGrants.SingleAsync(
            g => g.AccountId == accountId && g.WorkspaceId == workspaceId && g.UserId == userId);
        grant.RevokedAt.Should().BeNull("re-adding a member must clear the revocation");
        grant.MembershipStatus.Should().Be("Active");
        grant.RoleCodes.Should().BeEquivalentTo(["Admin"]);
        grant.IsWorkspaceAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task SyncAccountMemberGrant_KeepsAccountAndWorkspaceGrantsSeparate()
    {
        await using var context = _db.CreateContext(SystemTenant());
        var projection = new AccessGrantProjectionService(context);
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        await projection.SyncAccountMemberGrantAsync(accountId, userId, AccountRole.Member, FixedTime, CancellationToken.None);
        await projection.SyncWorkspaceMemberGrantAsync(accountId, workspaceId, userId, WorkspaceRole.Owner, FixedTime, CancellationToken.None);
        await context.SaveChangesAsync();

        var grants = await context.AccessGrants.Where(g => g.UserId == userId).ToListAsync();
        grants.Should().HaveCount(2, "account-level and workspace grants are distinct projection rows");
        grants.Should().Contain(g => g.WorkspaceId == null && g.SourceContext == "Account");
        grants.Should().Contain(g => g.WorkspaceId == workspaceId && g.SourceContext == "Workspace");
    }
}
