using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ArchiveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CreateChildItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveChildItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnarchiveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

/// <summary>
/// Sub-items: one level, in the parent's board and group, starting in the board's
/// default status, ordered under the parent, archived and restored with the parent.
/// </summary>
public class SubItemTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Board _board;
    private readonly BoardGroup _group;
    private readonly BoardItem _parent;
    private readonly BoardField _status;

    public SubItemTests()
    {
        _currentUser.Setup(u => u.UserId).Returns(TestUserId);
        _board = CreateBoard();
        _group = BoardGroup.Create(TestAccountId, TestWorkspaceId, _board.Id, "Group", Color.Create("#FF0000"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        _parent = CreateBoardItem(boardId: _board.Id, groupId: _group.Id);
        _status = BoardField.Create(TestAccountId, TestWorkspaceId, _board.Id, "Status", FieldType.Status,
            FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")), FractionalIndex.Create("a1"), TestUserId, TestNow, isSystem: true);
        _status.AddDefaultStatusOptions(TestUserId, TestNow);
        SetupBoards(_board);
        SetupBoardGroups(_group);
        SetupBoardFields(_status);
    }

    private CreateChildItemCommandHandler Creator() =>
        new(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, Placement, KeyAllocator);

    private List<BoardItem> Store(params BoardItem[] items)
    {
        var list = items.ToList();
        DbContextMock.Setup(c => c.BoardItems).Returns(CreateAsyncDbSet(list));
        DbContextMock.Setup(c => c.BoardItems.Add(It.IsAny<BoardItem>())).Callback<BoardItem>(list.Add);
        return list;
    }

    [Fact]
    public async Task CreatingAChild_PutsItInTheParentsGroupWithAKeyAndTheDefaultStatus()
    {
        var store = Store(_parent);

        var dto = await Creator().Handle(new CreateChildItemCommand(_parent.Id, " Write tests "), CancellationToken.None);

        var child = store.Single(i => i.Id == dto.Id);
        child.ParentItemId.Should().Be(_parent.Id);
        child.GroupId.Should().Be(_parent.GroupId);
        child.BoardId.Should().Be(_parent.BoardId);
        child.ItemLevel.Should().Be(1);
        child.ItemKey.Should().NotBeNullOrEmpty();
        dto.Title.Should().Be("Write tests");
        dto.Done.Should().BeFalse();
        var firstOption = _status.Options.OrderBy(o => o.Position).First();
        child.FieldValues.Single(v => v.FieldId == _status.Id).Value.Data.Value.Trim('"').Should().Be(firstOption.Id.ToString());
    }

    [Fact]
    public async Task ChildrenAreOrderedUnderTheirParentInCreationOrder()
    {
        var store = Store(_parent);

        var first = await Creator().Handle(new CreateChildItemCommand(_parent.Id, "One"), CancellationToken.None);
        var second = await Creator().Handle(new CreateChildItemCommand(_parent.Id, "Two"), CancellationToken.None);

        string.CompareOrdinal(first.Position, second.Position).Should().BeNegative();
        store.Count(i => i.ParentItemId == _parent.Id).Should().Be(2);
    }

    [Fact]
    public async Task AChildCannotHaveChildren()
    {
        var store = Store(_parent);
        var dto = await Creator().Handle(new CreateChildItemCommand(_parent.Id, "Child"), CancellationToken.None);

        var act = () => Creator().Handle(new CreateChildItemCommand(dto.Id, "Grandchild"), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Item_SubItemsAreOneLevel);
        store.Count.Should().Be(2);
    }

    [Fact]
    public async Task MovingAChildWithinItsParent_ChangesOnlyItsPlace()
    {
        var store = Store(_parent);
        var a = await Creator().Handle(new CreateChildItemCommand(_parent.Id, "A"), CancellationToken.None);
        var b = await Creator().Handle(new CreateChildItemCommand(_parent.Id, "B"), CancellationToken.None);
        var handler = new MoveChildItemCommandHandler(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object, Placement);

        var moved = await handler.Handle(new MoveChildItemCommand(b.Id, 1, PreviousSiblingId: null, NextSiblingId: a.Id), CancellationToken.None);

        string.CompareOrdinal(moved.Position, a.Position).Should().BeNegative();
        store.Single(i => i.Id == b.Id).GroupId.Should().Be(_parent.GroupId);
    }

    [Fact]
    public async Task AChildCannotBeMovedToAnotherGroupOnItsOwn()
    {
        var store = Store(_parent);
        var dto = await Creator().Handle(new CreateChildItemCommand(_parent.Id, "Child"), CancellationToken.None);
        var other = BoardGroup.Create(TestAccountId, TestWorkspaceId, _board.Id, "Other", Color.Create("#00FF00"), FractionalIndex.Create("a1"), TestUserId, TestNow);
        SetupBoardGroups(_group, other);
        var handler = new MoveBoardItemCommandHandler(new MoveBoardItemUseCase(DbContextMock.Object, DateTimeProviderMock.Object, Placement), _currentUser.Object);

        var act = () => handler.Handle(new MoveBoardItemCommand(dto.Id, other.Id, 1), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Item_SubItemFollowsParentGroup);
        store.Single(i => i.Id == dto.Id).GroupId.Should().Be(_group.Id);
    }

    [Fact]
    public async Task MovingTheParent_MovesItsChildrenToo()
    {
        var store = Store(_parent);
        var dto = await Creator().Handle(new CreateChildItemCommand(_parent.Id, "Child"), CancellationToken.None);
        var other = BoardGroup.Create(TestAccountId, TestWorkspaceId, _board.Id, "Other", Color.Create("#00FF00"), FractionalIndex.Create("a1"), TestUserId, TestNow);
        SetupBoardGroups(_group, other);
        var handler = new MoveBoardItemCommandHandler(new MoveBoardItemUseCase(DbContextMock.Object, DateTimeProviderMock.Object, Placement), _currentUser.Object);

        await handler.Handle(new MoveBoardItemCommand(_parent.Id, other.Id, 1), CancellationToken.None);

        store.Single(i => i.Id == dto.Id).GroupId.Should().Be(other.Id);
    }

    [Fact]
    public async Task ArchivingTheParentArchivesItsChildren_AndRestoringItRestoresOnlyThose()
    {
        var store = Store(_parent);
        var taken = await Creator().Handle(new CreateChildItemCommand(_parent.Id, "Archived with parent"), CancellationToken.None);
        var own = await Creator().Handle(new CreateChildItemCommand(_parent.Id, "Archived on its own"), CancellationToken.None);
        store.Single(i => i.Id == own.Id).Archive(TestUserId, TestNow);

        var archive = new ArchiveBoardItemCommandHandler(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object);
        await archive.Handle(new ArchiveBoardItemCommand(_parent.Id, _parent.Version), CancellationToken.None);

        _parent.IsArchived.Should().BeTrue();
        store.Single(i => i.Id == taken.Id).IsArchived.Should().BeTrue();
        store.Single(i => i.Id == taken.Id).ArchivedWithParent.Should().BeTrue();
        store.Single(i => i.Id == own.Id).ArchivedWithParent.Should().BeFalse();

        var unarchive = new UnarchiveBoardItemCommandHandler(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object);
        await unarchive.Handle(new UnarchiveBoardItemCommand(_parent.Id, ExpectedVersion: 1), CancellationToken.None);

        _parent.IsArchived.Should().BeFalse();
        store.Single(i => i.Id == taken.Id).IsArchived.Should().BeFalse();
        store.Single(i => i.Id == taken.Id).ArchivedWithParent.Should().BeFalse();
        store.Single(i => i.Id == own.Id).IsArchived.Should().BeTrue();
    }
}
