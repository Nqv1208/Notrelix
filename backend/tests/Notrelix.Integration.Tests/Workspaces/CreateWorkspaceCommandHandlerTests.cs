using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Workspaces;

[Collection("Database")]
public class CreateWorkspaceCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;
    private readonly Mock<ICurrentRequestContext> _requestContextMock;
    private readonly Mock<IDateTimeProvider> _dateTimeMock;

    public CreateWorkspaceCommandHandlerTests(PostgresTestContainer db)
    {
        _db = db;
        _requestContextMock = new Mock<ICurrentRequestContext>();
        _requestContextMock.Setup(r => r.RequireAccountId()).Returns(Guid.NewGuid());
        _requestContextMock.Setup(r => r.UserId).Returns(Guid.NewGuid());
        _dateTimeMock = new Mock<IDateTimeProvider>();
        _dateTimeMock.Setup(d => d.UtcNow).Returns(DateTimeOffset.UtcNow);
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
    public async Task Handle_WhenCreatingTeamWorkspace_ShouldSucceed()
    {
        await using var context = _db.CreateContext(SystemTenant());
        var userId = Guid.NewGuid();
        _requestContextMock.Setup(r => r.UserId).Returns(userId);

        var handler = new CreateWorkspaceCommandHandler(context, _requestContextMock.Object, _dateTimeMock.Object, new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)));
        var command = new CreateWorkspaceCommand("Awesome Project", "A great software project", false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        await context.SaveChangesAsync();

        var workspace = await context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == result.Data);

        workspace.Should().NotBeNull();
        workspace!.Name.Should().Be("Awesome Project");
        workspace.Slug.Should().Be("awesome-project");
        workspace.Description.Should().Be("A great software project");
        workspace.IsPersonal.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenCreatingPersonalWorkspace_ShouldSucceed()
    {
        await using var context = _db.CreateContext(SystemTenant());
        var userId = Guid.NewGuid();
        _requestContextMock.Setup(r => r.UserId).Returns(userId);

        var handler = new CreateWorkspaceCommandHandler(context, _requestContextMock.Object, _dateTimeMock.Object, new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)));
        var command = new CreateWorkspaceCommand("My Personal Tasks", null, true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        await context.SaveChangesAsync();

        var workspace = await context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == result.Data);

        workspace.Should().NotBeNull();
        workspace!.Name.Should().Be("My Personal Tasks");
        workspace.IsPersonal.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ShouldAppendUniqueSuffix()
    {
        await using var context = _db.CreateContext(SystemTenant());
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        _requestContextMock.Setup(r => r.RequireAccountId()).Returns(accountId);
        _requestContextMock.Setup(r => r.UserId).Returns(userId);
        _dateTimeMock.Setup(d => d.UtcNow).Returns(now);

        var existingWorkspace = Workspace.Create(accountId, userId, "Awesome Project", "awesome-project", now);
        context.Workspaces.Add(existingWorkspace);
        await context.SaveChangesAsync();

        var handler = new CreateWorkspaceCommandHandler(context, _requestContextMock.Object, _dateTimeMock.Object, new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)));
        var command = new CreateWorkspaceCommand("Awesome Project", "A duplicate project name", false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        result.Data.Should().NotBe(existingWorkspace.Id);

        await context.SaveChangesAsync();

        var duplicateWorkspace = await context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == result.Data);

        duplicateWorkspace.Should().NotBeNull();
        duplicateWorkspace!.Name.Should().Be("Awesome Project");
        duplicateWorkspace.Slug.Should().StartWith("awesome-project-");
        duplicateWorkspace.Slug.Length.Should().BeGreaterThan("awesome-project-".Length);
    }
}
