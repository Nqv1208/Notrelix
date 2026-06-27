using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Integration;

public class WorkspaceLifecycleTests
{
    [Fact]
    public async Task CreateWorkspace_WhenNonPersonal_StoresInDatabase()
    {
        await using var context = CreateContext();
        var userId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var currentUser = MockCurrentUser(userId);
        var clock = MockClock(now);

        var handler = new CreateWorkspaceCommandHandler(context, currentUser.Object, clock.Object);
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
        await using var context = CreateContext();
        var userId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var handler = new CreateWorkspaceCommandHandler(
            context, MockCurrentUser(userId).Object, MockClock(now).Object);
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
        await using var context = CreateContext();
        var userId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var handler = new CreateWorkspaceCommandHandler(
            context, MockCurrentUser(userId).Object, MockClock(now).Object);
        var command = new CreateWorkspaceCommand("Team Space", null, false);

        var result = await handler.Handle(command, default);
        result.Succeeded.Should().BeTrue();

        await context.SaveChangesAsync();

        var workspaceId = result.Data;
        var member = WorkspaceMember.Create(workspaceId, userId, WorkspaceRole.Admin, userId, now);
        context.WorkspaceMembers.Add(member);
        await context.SaveChangesAsync();

        var workspace = await context.Workspaces.FirstAsync(w => w.Id == workspaceId);
        var members = await context.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId).ToListAsync();

        workspace.Should().NotBeNull();
        members.Should().ContainSingle(m => m.UserId == userId && m.Role == WorkspaceRole.Admin);
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

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-workspace-lifecycle-{Guid.NewGuid():N}")
            .Options;
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        return new TestApplicationDbContext(options, currentWorkspace);
    }

    private sealed class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentWorkspace currentWorkspace)
            : base(options, currentWorkspace) { }
    }
}
