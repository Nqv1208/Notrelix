using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValues;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Items.Events;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.SharedKernel;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Boards;

[Collection("Database")]
public class UpdateBoardItemFieldValuesCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public UpdateBoardItemFieldValuesCommandHandlerTests(PostgresTestContainer db)
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
    public async Task Handle_ShouldRejectUserWithoutBoardEditPermission()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        await using var context = _db.CreateContext(currentWorkspace);
        var ownerId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var (boardItem, statusField, doneOption) = await SeedBoardAsync(context, ownerId, guestId, WorkspaceRole.Guest);
        var handler = CreateHandler(context, guestId);

        var act = () => handler.Handle(
            new UpdateBoardItemFieldValuesCommand(boardItem.Id, new Dictionary<Guid, object?> { [statusField.Id] = doneOption.Id.ToString() }),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_ShouldUseDomainBehaviorWhenUpdatingStatusField()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        await using var context = _db.CreateContext(currentWorkspace);
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var (boardItem, statusField, doneOption) = await SeedBoardAsync(context, ownerId, memberId, WorkspaceRole.Member);
        var handler = CreateHandler(context, memberId);

        var result = await handler.Handle(
            new UpdateBoardItemFieldValuesCommand(boardItem.Id, new Dictionary<Guid, object?> { [statusField.Id] = doneOption.Id.ToString() }),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        boardItem.DomainEvents.Should().ContainSingle(item => item is BoardItemFieldValueChangedDomainEvent);
    }

    private static async Task<(BoardItem BoardItem, BoardField StatusField, FieldOption DoneOption)> SeedBoardAsync(
        ApplicationDbContext context,
        Guid ownerId,
        Guid userId,
        WorkspaceRole userRole)
    {
        var now = DateTimeOffset.UtcNow;
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Workspace", "workspace", now);
        var workspaceMember = WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, userId, userRole, ownerId, now);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Board", null, now);
        var group = BoardGroup.Create(Guid.NewGuid(), workspace.Id, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), ownerId, now);
        var boardItem = BoardItem.Create(Guid.NewGuid(), workspace.Id, board.Id, group.Id, "Task", FractionalIndex.Create("a0"), ownerId, now);
        var statusField = BoardField.Create(Guid.NewGuid(), workspace.Id, board.Id, "Status", FieldType.Status, FieldSettings.Empty(), FractionalIndex.Create("a0"), ownerId, now);

        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(workspaceMember);
        context.Boards.Add(board);
        context.BoardGroups.Add(group);
        context.BoardItems.Add(boardItem);
        context.BoardFields.Add(statusField);
        await context.SaveChangesAsync();

        var doneOption = FieldOption.Create(statusField.Id, "Done", Color.Create("#00FF00"), FractionalIndex.Create("a1"));
        context.FieldOptions.Add(doneOption);
        await context.SaveChangesAsync();

        boardItem.ClearDomainEvents();

        return (boardItem, statusField, doneOption);
    }

    private UpdateBoardItemFieldValuesCommandHandler CreateHandler(ApplicationDbContext context, Guid userId)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(item => item.UserId).Returns(userId);
        var timeProvider = new Mock<IDateTimeProvider>();
        timeProvider.Setup(t => t.UtcNow).Returns(DateTimeOffset.UtcNow);
        var evaluator = new PermissionService(context, context, context, timeProvider.Object);
        var permissions = new WorkspacePermissionService(evaluator, context);

        return new UpdateBoardItemFieldValuesCommandHandler(context, currentUser.Object, permissions, timeProvider.Object, Mock.Of<IResourceReferenceResolver>());
    }
}
