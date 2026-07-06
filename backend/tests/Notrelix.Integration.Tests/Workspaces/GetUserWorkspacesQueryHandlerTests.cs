using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetUserWorkspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Workspaces;

[Collection("Database")]
public class GetUserWorkspacesQueryHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public GetUserWorkspacesQueryHandlerTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Handle_returns_active_workspaces_for_user_with_member_counts()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var userId = Guid.NewGuid();
        var teammateId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var ownedWorkspace = Workspace.Create(Guid.NewGuid(), userId, "Owned Workspace", "owned-workspace", now);
        context.Workspaces.Add(ownedWorkspace);

        var ownedMember = WorkspaceMember.Create(Guid.NewGuid(), ownedWorkspace.Id, userId, WorkspaceRole.Owner, userId, now);
        var ownedTeammate = WorkspaceMember.Create(Guid.NewGuid(), ownedWorkspace.Id, teammateId, WorkspaceRole.Member, userId, now);
        context.WorkspaceMembers.Add(ownedMember);
        context.WorkspaceMembers.Add(ownedTeammate);

        var joinedWorkspace = Workspace.Create(Guid.NewGuid(), otherOwnerId, "Joined Workspace", "joined-workspace", now);
        context.Workspaces.Add(joinedWorkspace);

        var joinedOwner = WorkspaceMember.Create(Guid.NewGuid(), joinedWorkspace.Id, otherOwnerId, WorkspaceRole.Owner, otherOwnerId, now);
        var joinedMember = WorkspaceMember.Create(Guid.NewGuid(), joinedWorkspace.Id, userId, WorkspaceRole.Admin, otherOwnerId, now);
        context.WorkspaceMembers.Add(joinedOwner);
        context.WorkspaceMembers.Add(joinedMember);

        var archivedWorkspace = Workspace.Create(Guid.NewGuid(), userId, "Archived Workspace", "archived-workspace", now);
        archivedWorkspace.Archive(userId, now);
        context.Workspaces.Add(archivedWorkspace);
        var archivedMember = WorkspaceMember.Create(Guid.NewGuid(), archivedWorkspace.Id, userId, WorkspaceRole.Owner, userId, now);
        context.WorkspaceMembers.Add(archivedMember);

        var unrelatedWorkspace = Workspace.Create(Guid.NewGuid(), otherOwnerId, "Unrelated Workspace", "unrelated-workspace", now);
        context.Workspaces.Add(unrelatedWorkspace);
        var unrelatedMember = WorkspaceMember.Create(Guid.NewGuid(), unrelatedWorkspace.Id, otherOwnerId, WorkspaceRole.Owner, otherOwnerId, now);
        context.WorkspaceMembers.Add(unrelatedMember);

        await context.SaveChangesAsync();

        var handler = new GetUserWorkspacesQueryHandler(context);

        var result = await handler.Handle(new GetUserWorkspacesQuery(userId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Select(w => w.Slug)
            .Should()
            .BeEquivalentTo(["joined-workspace", "owned-workspace"]);

        result.Data!.Single(w => w.Slug == "owned-workspace")
            .MemberCount.Should().Be(2);
        result.Data!.Single(w => w.Slug == "joined-workspace")
            .MemberCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_rejects_empty_user_id()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var handler = new GetUserWorkspacesQueryHandler(context);

        var result = await handler.Handle(new GetUserWorkspacesQuery(Guid.Empty), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle("User is not authenticated");
    }
}
