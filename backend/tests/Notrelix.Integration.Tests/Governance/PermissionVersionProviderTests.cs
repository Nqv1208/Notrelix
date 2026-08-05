using Microsoft.Extensions.Logging;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Governance.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Governance;

[Collection("Database")]
public class PermissionVersionProviderTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public PermissionVersionProviderTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private (ApplicationDbContext Context, PermissionVersionProvider Provider) CreateFixture()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        var context = _db.CreateContext(tenant);
        var logger = new Mock<ILogger<PermissionVersionProvider>>();
        var provider = new PermissionVersionProvider(context, logger.Object);
        return (context, provider);
    }

    [Fact]
    public async Task GetVersionAsync_VersionDiffersByUser()
    {
        var (context, provider) = CreateFixture();
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, memberId, WorkspaceRole.Member, ownerId, Now));
        await context.SaveChangesAsync();

        var versionOwner = await provider.GetVersionAsync(accountId, workspace.Id, ownerId, default);
        var versionMember = await provider.GetVersionAsync(accountId, workspace.Id, memberId, default);

        versionOwner.Should().NotBe(versionMember);
        versionOwner.Should().Contain(accountId.ToString());
        versionOwner.Should().Contain(workspace.Id.ToString());
        versionOwner.Should().Contain(ownerId.ToString());
        versionMember.Should().Contain(memberId.ToString());
    }

    [Fact]
    public async Task GetVersionAsync_VersionChangesWhenMembershipChanges()
    {
        var (context, provider) = CreateFixture();
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        await context.SaveChangesAsync();

        // Can compute version before adding more members
        var versionOwner = await provider.GetVersionAsync(accountId, workspace.Id, ownerId, default);
        versionOwner.Should().Contain(accountId.ToString());
        versionOwner.Should().Contain(workspace.Id.ToString());
        versionOwner.Should().Contain(ownerId.ToString());

        // Adding a second member produces a valid version for the new user
        var memberId = Guid.NewGuid();
        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, memberId, WorkspaceRole.Member, ownerId, Now));
        await context.SaveChangesAsync();

        var versionMember = await provider.GetVersionAsync(accountId, workspace.Id, memberId, default);
        versionMember.Should().Contain(memberId.ToString());
        versionMember.Should().NotBe(versionOwner);
    }

    [Fact]
    public async Task GetVersionAsync_VersionChangesWhenRoleAssignmentChanges()
    {
        var (context, provider) = CreateFixture();
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        await context.SaveChangesAsync();

        var versionBefore = await provider.GetVersionAsync(accountId, workspace.Id, ownerId, default);

        var memberId = Guid.NewGuid();
        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, memberId, WorkspaceRole.Member, ownerId, Now));
        var customRole = CustomRole.Create(accountId, workspace.Id, "Editor", null, ownerId, Now);
        context.Set<CustomRole>().Add(customRole);
        await context.SaveChangesAsync();
        var assignment = MemberRoleAssignment.Create(accountId, workspace.Id, memberId, customRole.Id, Now);
        context.Set<MemberRoleAssignment>().Add(assignment);
        await context.SaveChangesAsync();

        var versionAfter = await provider.GetVersionAsync(accountId, workspace.Id, ownerId, default);

        versionBefore.Should().NotBe(versionAfter);
    }

    [Fact]
    public async Task GetVersionAsync_VersionChangesWhenResourcePermissionChanges()
    {
        var (context, provider) = CreateFixture();
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        await context.SaveChangesAsync();

        var versionBefore = await provider.GetVersionAsync(accountId, workspace.Id, ownerId, default);

        var permission = ResourcePermission.Grant(
            accountId, workspace.Id, ResourceKind.Create("work-management.board"),
            Guid.NewGuid(), PermissionSubjectType.User,
            ownerId, PermissionLevel.Editor, PermissionLevel.Owner, ownerId, Now);
        permission.ChangeLevel(PermissionLevel.Manager, ownerId, Now);
        context.ResourcePermissions.Add(permission);
        await context.SaveChangesAsync();

        var versionAfter = await provider.GetVersionAsync(accountId, workspace.Id, ownerId, default);

        versionBefore.Should().NotBe(versionAfter);
    }

    [Fact]
    public async Task GetVersionAsync_VersionIncludesAccountDimension()
    {
        var (context, provider) = CreateFixture();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspaceA = Workspace.Create(accountA, ownerId, "Account A WS", "aws", Now);
        var workspaceB = Workspace.Create(accountB, ownerId, "Account B WS", "bws", Now);
        context.Workspaces.Add(workspaceA);
        context.Workspaces.Add(workspaceB);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspaceA.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(Guid.NewGuid(), workspaceB.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        await context.SaveChangesAsync();

        var versionA = await provider.GetVersionAsync(accountA, workspaceA.Id, ownerId, default);
        var versionB = await provider.GetVersionAsync(accountB, workspaceB.Id, ownerId, default);

        versionA.Should().Contain(accountA.ToString());
        versionB.Should().Contain(accountB.ToString());
    }

    [Fact]
    public async Task GetVersionAsync_ReturnsFallbackVersionWhenNoDataExists()
    {
        var (context, provider) = CreateFixture();
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var version = await provider.GetVersionAsync(accountId, workspaceId, userId, default);

        version.Should().NotBeNull();
        version.Should().Contain(accountId.ToString());
        version.Should().Contain(workspaceId.ToString());
        version.Should().Contain(userId.ToString());
    }
}
