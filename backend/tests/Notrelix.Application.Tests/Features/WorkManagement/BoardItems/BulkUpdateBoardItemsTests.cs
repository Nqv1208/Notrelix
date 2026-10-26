using Notrelix.Domain.Governance.Permissions;using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ArchiveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.AssignBoardItemMember;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.BulkUpdateBoardItems;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemStatus;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class BulkUpdateBoardItemsTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IRequestHandler<UpdateBoardItemStatusCommand, Result>> _status = new();
    private readonly Mock<IRequestHandler<ArchiveBoardItemCommand, Result>> _archive = new();
    private readonly Mock<IRequestHandler<AssignBoardItemMemberCommand, Result>> _assign = new();
    private readonly BulkUpdateBoardItemsCommandHandler _handler;
    private readonly Guid _boardId = Guid.CreateVersion7();

    public BulkUpdateBoardItemsTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _status.Setup(h => h.Handle(It.IsAny<UpdateBoardItemStatusCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _archive.Setup(h => h.Handle(It.IsAny<ArchiveBoardItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _assign.Setup(h => h.Handle(It.IsAny<AssignBoardItemMemberCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        SetupBoardItemMembers();
        _handler = new BulkUpdateBoardItemsCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object,
            _status.Object,
            _archive.Object,
            _assign.Object);
    }

    private BoardItem[] ItemsOnBoard(int count)
    {
        var items = Enumerable.Range(0, count).Select(i => CreateBoardItem(boardId: _boardId, position: $"a{i}")).ToArray();
        SetupBoardItems(items);
        return items;
    }

    [Fact]
    public async Task Handle_SetStatus_WritesEveryItemThroughTheStatusCommand()
    {
        var items = ItemsOnBoard(3);

        var result = await _handler.Handle(
            new BulkUpdateBoardItemsCommand(_boardId, BulkBoardItemOperations.SetStatus, items.Select(i => i.Id).ToList(), Status: "opt-done"),
            CancellationToken.None);

        result.Updated.Should().Be(3);
        foreach (var item in items)
            _status.Verify(h => h.Handle(It.Is<UpdateBoardItemStatusCommand>(c => c.BoardItemId == item.Id && c.Status == "opt-done"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SetDueDate_MovesEveryDueDate()
    {
        var items = ItemsOnBoard(2);
        var due = new DateTime(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc);

        await _handler.Handle(
            new BulkUpdateBoardItemsCommand(_boardId, BulkBoardItemOperations.SetDueDate, items.Select(i => i.Id).ToList(), DueDate: due),
            CancellationToken.None);

        items.Should().OnlyContain(i => i.DueAt == new DateTimeOffset(due, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ClearDueDate_RemovesTheDueDateOfEveryItem()
    {
        var items = ItemsOnBoard(2);
        var ids = items.Select(i => i.Id).ToList();
        await _handler.Handle(
            new BulkUpdateBoardItemsCommand(_boardId, BulkBoardItemOperations.SetDueDate, ids, DueDate: new DateTime(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        await _handler.Handle(new BulkUpdateBoardItemsCommand(_boardId, BulkBoardItemOperations.ClearDueDate, ids), CancellationToken.None);

        items.Should().OnlyContain(i => i.DueAt == null);
    }

    [Fact]
    public async Task Handle_ItemOfAnotherBoard_IsNotFoundAndNothingIsWritten()
    {
        var mine = CreateBoardItem(boardId: _boardId);
        var foreign = CreateBoardItem(boardId: Guid.CreateVersion7());
        SetupBoardItems(mine, foreign);

        await _handler.Invoking(h => h.Handle(
                new BulkUpdateBoardItemsCommand(_boardId, BulkBoardItemOperations.Archive, [mine.Id, foreign.Id]),
                CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
        _archive.Verify(h => h.Handle(It.IsAny<ArchiveBoardItemCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AnItemTheCommandRefuses_ThrowsSoTheWholeRequestRollsBack()
    {
        var items = ItemsOnBoard(2);
        _status.Setup(h => h.Handle(It.Is<UpdateBoardItemStatusCommand>(c => c.BoardItemId == items[1].Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("No status field found on this board."));

        await _handler.Invoking(h => h.Handle(
                new BulkUpdateBoardItemsCommand(_boardId, BulkBoardItemOperations.SetStatus, items.Select(i => i.Id).ToList(), Status: "opt"),
                CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Handle_Assign_AddsTheMemberToEveryItem()
    {
        var items = ItemsOnBoard(2);
        var user = Guid.CreateVersion7();

        await _handler.Handle(
            new BulkUpdateBoardItemsCommand(_boardId, BulkBoardItemOperations.Assign, items.Select(i => i.Id).ToList(), AssigneeUserId: user),
            CancellationToken.None);

        foreach (var item in items)
            _assign.Verify(h => h.Handle(It.Is<AssignBoardItemMemberCommand>(c => c.BoardItemId == item.Id && c.UserId == user), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(BulkBoardItemOperations.Assign, true)]
    [InlineData(BulkBoardItemOperations.UnassignAll, true)]
    [InlineData(BulkBoardItemOperations.SetStatus, false)]
    [InlineData(BulkBoardItemOperations.SetDueDate, false)]
    [InlineData(BulkBoardItemOperations.ClearDueDate, false)]
    [InlineData(BulkBoardItemOperations.Archive, false)]
    public void Authorization_NeedsUpdateItemOnTheBoard_AndAssignItemToo_WhenAssigneesChange(string operation, bool needsAssign)
    {
        var command = new BulkUpdateBoardItemsCommand(_boardId, operation, [Guid.CreateVersion7()]);

        command.Action.Should().Be(PermissionAction.UpdateItem);
        command.AdditionalActionNames.Should().BeEquivalentTo(needsAssign ? new[] { "AssignItem" } : Array.Empty<string>());
        command.Resource.ResourceId.Should().Be(_boardId);
    }

    [Fact]
    public void Validator_RejectsEmptyOversizedAndUnknownRequests()
    {
        var validator = new BulkUpdateBoardItemsCommandValidator();
        var one = new List<Guid> { Guid.CreateVersion7() };

        validator.Validate(new BulkUpdateBoardItemsCommand(_boardId, BulkBoardItemOperations.Archive, one)).IsValid.Should().BeTrue();
        validator.Validate(new BulkUpdateBoardItemsCommand(_boardId, BulkBoardItemOperations.Archive, [])).IsValid.Should().BeFalse();
        validator.Validate(new BulkUpdateBoardItemsCommand(_boardId, "explode", one)).IsValid.Should().BeFalse();
        validator.Validate(new BulkUpdateBoardItemsCommand(_boardId, BulkBoardItemOperations.SetStatus, one)).IsValid.Should().BeFalse();
        validator.Validate(new BulkUpdateBoardItemsCommand(_boardId, BulkBoardItemOperations.Assign, one)).IsValid.Should().BeFalse();
        validator.Validate(new BulkUpdateBoardItemsCommand(
            _boardId, BulkBoardItemOperations.Archive,
            Enumerable.Range(0, BulkBoardItemOperations.MaxItems + 1).Select(_ => Guid.CreateVersion7()).ToList())).IsValid.Should().BeFalse();
    }
}
