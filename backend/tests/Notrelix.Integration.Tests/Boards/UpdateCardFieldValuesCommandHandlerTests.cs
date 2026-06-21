using Microsoft.EntityFrameworkCore;
using Moq;
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

namespace Notrelix.Integration.Tests.Boards;

public class UpdateBoardItemFieldValuesCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldRejectUserWithoutBoardEditPermission()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var (boardItem, statusField) = await SeedBoardAsync(context, ownerId, guestId, WorkspaceRole.Guest);
        var handler = CreateHandler(context, guestId);

        var act = () => handler.Handle(
            new UpdateBoardItemFieldValuesCommand(boardItem.Id, new Dictionary<Guid, object?> { [statusField.Id] = "done" }),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_ShouldUseDomainBehaviorWhenUpdatingStatusField()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var (boardItem, statusField) = await SeedBoardAsync(context, ownerId, memberId, WorkspaceRole.Member);
        var handler = CreateHandler(context, memberId);

        var result = await handler.Handle(
            new UpdateBoardItemFieldValuesCommand(boardItem.Id, new Dictionary<Guid, object?> { [statusField.Id] = "done" }),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        boardItem.DomainEvents.Should().ContainSingle(item => item is BoardItemFieldValueChangedDomainEvent);
    }

    private static async Task<(BoardItem BoardItem, BoardField StatusField)> SeedBoardAsync(
        ApplicationDbContext context,
        Guid ownerId,
        Guid userId,
        WorkspaceRole userRole)
    {
        var now = DateTimeOffset.UtcNow;
        var workspace = Workspace.Create(ownerId, "Workspace", "workspace", now);
        var workspaceMember = WorkspaceMember.Create(workspace.Id, userId, userRole, ownerId, now);
        var board = Board.Create(workspace.Id, ownerId, "Board", null, now);
        var group = BoardGroup.Create(workspace.Id, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), ownerId, now);
        var boardItem = BoardItem.Create(workspace.Id, board.Id, group.Id, "Task", FractionalIndex.Create("a0"), ownerId, now);
        var statusField = BoardField.Create(workspace.Id, board.Id, "Status", FieldType.Status, FieldSettings.Empty(), FractionalIndex.Create("a0"), ownerId, now);

        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(workspaceMember);
        context.Boards.Add(board);
        context.BoardGroups.Add(group);
        context.BoardItems.Add(boardItem);
        context.BoardFields.Add(statusField);
        await context.SaveChangesAsync();
        boardItem.ClearDomainEvents();

        return (boardItem, statusField);
    }

    private static UpdateBoardItemFieldValuesCommandHandler CreateHandler(ApplicationDbContext context, Guid userId)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(item => item.UserId).Returns(userId);
        var timeProvider = new Mock<IDateTimeProvider>();
        timeProvider.Setup(t => t.UtcNow).Returns(DateTimeOffset.UtcNow);
        var evaluator = new PermissionService(context, timeProvider.Object);
        var permissions = new WorkspacePermissionService(evaluator, context);

        return new UpdateBoardItemFieldValuesCommandHandler(context, currentUser.Object, permissions, timeProvider.Object);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-card-fields-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
