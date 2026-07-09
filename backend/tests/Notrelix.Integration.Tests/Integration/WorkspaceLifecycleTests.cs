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
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var userId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var requestContext = MockRequestContext(userId, Guid.NewGuid());
        var clock = MockClock(now);

        var handler = new CreateWorkspaceCommandHandler(context, requestContext.Object, clock.Object);
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
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var userId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var requestContext = MockRequestContext(userId, Guid.NewGuid());

        var handler = new CreateWorkspaceCommandHandler(
            context, requestContext.Object, MockClock(now).Object);
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
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var userId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var requestContext = MockRequestContext(userId, Guid.NewGuid());

        var handler = new CreateWorkspaceCommandHandler(
            context, requestContext.Object, MockClock(now).Object);
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

    private static Mock<ICurrentRequestContext> MockRequestContext(Guid userId, Guid accountId)
    {
        var mock = new Mock<ICurrentRequestContext>();
        mock.Setup(x => x.UserId).Returns(userId);
        mock.Setup(x => x.RequireAccountId()).Returns(accountId);
        return mock;
    }

    private static Mock<IDateTimeProvider> MockClock(DateTimeOffset now)
    {
        var mock = new Mock<IDateTimeProvider>();
        mock.Setup(x => x.UtcNow).Returns(now);
        return mock;
    }
}
