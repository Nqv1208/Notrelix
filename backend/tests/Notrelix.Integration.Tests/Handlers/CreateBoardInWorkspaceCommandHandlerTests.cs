using Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Handlers;

[Collection("Database")]
public class CreateBoardInWorkspaceCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public CreateBoardInWorkspaceCommandHandlerTests(PostgresTestContainer db)
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
    public async Task Handle_ShouldCreateBoard_WithDefaultFields()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var accountId = Guid.NewGuid();

        var workspace = Workspace.Create(accountId, userId, "Test", "test", now);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        tenant.SetWorkspace(accountId, workspace.Id, null);

        var requestContextMock = new Mock<ICurrentRequestContext>();
        requestContextMock.Setup(r => r.RequireAccountId()).Returns(accountId);
        requestContextMock.Setup(r => r.UserId).Returns(userId);

        var handler = new CreateBoardInWorkspaceCommandHandler(
            context, requestContextMock.Object, FakeDateTimeProvider.WithFixedTime(now));

        var result = await handler.Handle(
            new CreateBoardInWorkspaceCommand(workspace.Id, "My Board", null, null, null),
            CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var board = await context.Boards.FirstOrDefaultAsync(b => b.Id == result.Data);
        board.Should().NotBeNull();
        board!.Title.Should().Be("My Board");
        board.WorkspaceId.Should().Be(workspace.Id);
        board.AccountId.Should().Be(accountId);

        var fields = await context.BoardFields.Where(f => f.BoardId == board.Id).ToListAsync();
        fields.Should().HaveCount(4);
        fields.Should().AllSatisfy(f => f.AccountId.Should().Be(accountId));
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ShouldReturnSuccess_WithoutSaving()
    {
        // Workspace existence is validated by WorkspaceContextBehavior, not the handler.
        // The handler creates the board entity in-memory and returns Success.
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);

        var requestContextMock = new Mock<ICurrentRequestContext>();
        requestContextMock.Setup(r => r.RequireAccountId()).Returns(Guid.NewGuid());
        requestContextMock.Setup(r => r.UserId).Returns(Guid.NewGuid());

        var handler = new CreateBoardInWorkspaceCommandHandler(
            context, requestContextMock.Object, FakeDateTimeProvider.WithFixedTime(DateTimeOffset.UtcNow));

        var result = await handler.Handle(
            new CreateBoardInWorkspaceCommand(Guid.NewGuid(), "Board", null, null, null), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldCreateBoard_WithCustomVisibility()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var accountId = Guid.NewGuid();

        var workspace = Workspace.Create(accountId, userId, "Test", "test", now);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        tenant.SetWorkspace(accountId, workspace.Id, null);

        var requestContextMock = new Mock<ICurrentRequestContext>();
        requestContextMock.Setup(r => r.RequireAccountId()).Returns(accountId);
        requestContextMock.Setup(r => r.UserId).Returns(userId);

        var handler = new CreateBoardInWorkspaceCommandHandler(
            context, requestContextMock.Object, FakeDateTimeProvider.WithFixedTime(now));

        var result = await handler.Handle(
            new CreateBoardInWorkspaceCommand(workspace.Id, "Private Board", null, null, BoardVisibility.Private),
            CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        var board = await context.Boards.FirstOrDefaultAsync(b => b.Id == result.Data);
        board.Should().NotBeNull();
        board!.Visibility.Should().Be(BoardVisibility.Private);
    }
}
