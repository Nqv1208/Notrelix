using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Checklists;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DuplicateBoardItem;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class DuplicateBoardItemTests : WorkManagementHandlerTestBase
{
    private readonly DuplicateBoardItemCommandHandler _handler;

    public DuplicateBoardItemTests()
    {
        _handler = new DuplicateBoardItemCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object,
            Placement,
            KeyAllocator);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesDuplicate()
    {
        var board = CreateBoard();
        var item = CreateBoardItem(boardId: board.Id);
        SetupBoards(board);
        SetupBoardItems(item);

        var command = new DuplicateBoardItemCommand(item.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        var command = new DuplicateBoardItemCommand(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedItem_ThrowsNotFoundException()
    {
        var item = CreateBoardItem();
        item.Delete(TestUserId, TestNow);
        SetupBoardItems(item);

        var command = new DuplicateBoardItemCommand(item.Id);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    private (BoardItem Source, BoardField Field, Guid LabelId, Guid Assignee) RichSource()
    {
        var board = CreateBoard();
        var source = CreateBoardItem(boardId: board.Id);
        var field = BoardField.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Ghi chú",
            FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), TestUserId, TestNow);
        source.UpdateFieldValue(field, FieldValue.Create(JsonValue.Create(JsonSerializer.Serialize("hello"))), TestUserId, TestNow);
        var child = BoardItem.CreateSubItem(source, "Việc con", FractionalIndex.Create("a0"), TestUserId, TestNow);
        var checklist = Checklist.Create(TestAccountId, TestWorkspaceId, source.Id, "Kiểm tra", FractionalIndex.Create("a0"), TestUserId, TestNow);
        var entry = checklist.AddItem("Bước một", FractionalIndex.Create("a0"), TestUserId, TestNow);
        checklist.SetItemCompletion(entry.Id, true, TestUserId, TestNow);
        var labelId = Guid.CreateVersion7();
        var assignee = Guid.CreateVersion7();
        SetupBoards(board);
        SetupBoardItems(source, child);
        SetupBoardFields(field);
        SetupChecklists(checklist);
        SetupBoardItemLabels(BoardItemLabel.Create(TestAccountId, TestWorkspaceId, board.Id, source.Id, labelId, TestUserId, TestNow));
        SetupBoardItemMembers(CreateBoardItemMember(itemId: source.Id, userId: assignee));
        return (source, field, labelId, assignee);
    }

    private BoardItem TheCopy(BoardItem source, Guid id) =>
        DbContextMock.Object.BoardItems.Single(i => i.Id == id && i.Id != source.Id);

    [Fact]
    public async Task Handle_WithoutOptions_CopiesOnlyNameDatesAndDescription()
    {
        var (source, _, _, _) = RichSource();

        var result = await _handler.Handle(new DuplicateBoardItemCommand(source.Id), CancellationToken.None);

        var copy = TheCopy(source, result.Data);
        copy.Name.Should().Be("Test Item copy");
        copy.FieldValues.Should().BeEmpty();
        DbContextMock.Object.BoardItems.Count(i => i.ParentItemId == copy.Id).Should().Be(0);
        DbContextMock.Object.Checklists.Count(c => c.ItemId == copy.Id).Should().Be(0);
        DbContextMock.Object.BoardItemLabels.Count(l => l.ItemId == copy.Id).Should().Be(0);
        DbContextMock.Object.BoardItemMembers.Count(m => m.ItemId == copy.Id).Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithEveryOption_CopiesValuesChecklistsSubItemsLabelsAndAssignees()
    {
        var (source, field, labelId, assignee) = RichSource();
        var options = new DuplicateBoardItemOptions(true, true, true, true, true);

        var result = await _handler.Handle(new DuplicateBoardItemCommand(source.Id, "  Bản chép  ", options), CancellationToken.None);

        var copy = TheCopy(source, result.Data);
        copy.Name.Should().Be("Bản chép");
        copy.FieldValues.Should().ContainSingle(v => v.FieldId == field.Id);
        DbContextMock.Object.BoardItems.Should().ContainSingle(i => i.ParentItemId == copy.Id && i.Name == "Việc con");
        var checklist = DbContextMock.Object.Checklists.Single(c => c.ItemId == copy.Id);
        checklist.Title.Should().Be("Kiểm tra");
        checklist.Items.Should().ContainSingle(e => e.Title == "Bước một" && !e.IsDone);
        DbContextMock.Object.BoardItemLabels.Should().ContainSingle(l => l.ItemId == copy.Id && l.LabelId == labelId);
        DbContextMock.Object.BoardItemMembers.Should().ContainSingle(m => m.ItemId == copy.Id && m.UserId == assignee);
        // The source keeps what it had.
        DbContextMock.Object.Checklists.Count(c => c.ItemId == source.Id).Should().Be(1);
        DbContextMock.Object.BoardItemMembers.Count(m => m.ItemId == source.Id).Should().Be(1);
    }

    [Fact]
    public async Task Handle_SubItem_IsRefusedBecauseItWouldPointAtTheOriginalParent()
    {
        var (source, _, _, _) = RichSource();
        var child = DbContextMock.Object.BoardItems.Single(i => i.ParentItemId == source.Id);

        await _handler.Invoking(h => h.Handle(new DuplicateBoardItemCommand(child.Id), CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public void Validator_RejectsATooLongName()
    {
        new DuplicateBoardItemCommandValidator()
            .Validate(new DuplicateBoardItemCommand(Guid.CreateVersion7(), new string('x', 201)))
            .IsValid.Should().BeFalse();
    }
}
