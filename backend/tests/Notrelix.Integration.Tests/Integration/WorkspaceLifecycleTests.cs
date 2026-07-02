using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Integration;

[Collection("Database")]
public class WorkspaceLifecycleTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public WorkspaceLifecycleTests(PostgresTestContainer db)
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
    public async Task CreateWorkspace_WhenNonPersonal_StoresInDatabase()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        await using var context = _db.CreateContext(currentWorkspace);
        var userId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var currentUser = MockCurrentUser(userId);
        var tenant = new FakeCurrentTenantContext();
        tenant.SetAccount(Guid.NewGuid(), userId);
        var clock = MockClock(now);

        var handler = new CreateWorkspaceCommandHandler(context, currentUser.Object, tenant, clock.Object);
        var command = new CreateWorkspaceCommand("Integration Workspace", "Phase 3 test", false);

        var result = await handler.Handle(command, default);

        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        var workspace = await context.Workspaces.FirstAsync(w => w.Id == result.Data);
        workspace.Name.Should().Be("Integration Workspace");
        workspace.Status.Should().Be(WorkspaceStatus.Active);
        workspace.IsPersonal.Should().BeFalse();
    }

    [Fact]
    public async Task CreateWorkspace_WhenPersonal_SetsIsPersonalFlag()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        await using var context = _db.CreateContext(currentWorkspace);
        var userId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var tenant = new FakeCurrentTenantContext();
        tenant.SetAccount(Guid.NewGuid(), userId);

        var handler = new CreateWorkspaceCommandHandler(
            context, MockCurrentUser(userId).Object, tenant, MockClock(now).Object);
        var command = new CreateWorkspaceCommand("Personal Tasks", null, true);

        var result = await handler.Handle(command, default);

        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        var workspace = await context.Workspaces.FirstAsync(w => w.Id == result.Data);
        workspace.IsPersonal.Should().BeTrue();
    }

    [Fact]
    public async Task WorkspaceWithMembers_CanQueryBothAggregates()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        await using var context = _db.CreateContext(currentWorkspace);
        var userId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var tenant = new FakeCurrentTenantContext();
        tenant.SetAccount(Guid.NewGuid(), userId);

        var handler = new CreateWorkspaceCommandHandler(
            context, MockCurrentUser(userId).Object, tenant, MockClock(now).Object);
        var command = new CreateWorkspaceCommand("Team Space", null, false);

        var result = await handler.Handle(command, default);
        result.Succeeded.Should().BeTrue();

        await context.SaveChangesAsync();

        var workspaceId = result.Data;
        var adminUserName = Guid.CreateVersion7();
        var member = WorkspaceMember.Create(Guid.NewGuid(), workspaceId, adminUserName, WorkspaceRole.Admin, userId, now);
        context.WorkspaceMembers.Add(member);
        await context.SaveChangesAsync();

        var workspace = await context.Workspaces.FirstAsync(w => w.Id == workspaceId);
        var members = await context.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId).ToListAsync();

        workspace.Should().NotBeNull();
        members.Should().Contain(m => m.UserId == adminUserName && m.Role == WorkspaceRole.Admin);
    }

    private static Mock<ICurrentUser> MockCurrentUser(Guid userId)
    {
        var mock = new Mock<ICurrentUser>();
        mock.Setup(x => x.UserId).Returns(userId);
        return mock;
    }

    private static Mock<IDateTimeProvider> MockClock(DateTimeOffset now)
    {
        var mock = new Mock<IDateTimeProvider>();
        mock.Setup(x => x.UtcNow).Returns(now);
        return mock;
    }
}
