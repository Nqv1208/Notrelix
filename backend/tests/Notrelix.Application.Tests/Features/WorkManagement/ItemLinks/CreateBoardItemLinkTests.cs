using Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.CreateBoardItemLink;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.ItemLinks;

public class CreateBoardItemLinkTests : WorkManagementHandlerTestBase
{
    private readonly CreateBoardItemLinkCommandHandler _handler;

    public CreateBoardItemLinkTests()
    {
        _handler = new CreateBoardItemLinkCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesLink()
    {
        var sourceItem = CreateBoardItem();
        var targetItem = CreateBoardItem(boardId: sourceItem.BoardId);
        SetupBoardItems(sourceItem, targetItem);

        var command = new CreateBoardItemLinkCommand(sourceItem.Id, targetItem.Id, "related");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_SourceItemNotFound_ThrowsNotFoundException()
    {
        var targetItem = CreateBoardItem();
        SetupBoardItems(targetItem);

        var command = new CreateBoardItemLinkCommand(Guid.CreateVersion7(), targetItem.Id, "related");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_TargetItemNotFound_ThrowsNotFoundException()
    {
        var sourceItem = CreateBoardItem();
        SetupBoardItems(sourceItem);

        var command = new CreateBoardItemLinkCommand(sourceItem.Id, Guid.CreateVersion7(), "related");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DifferentWorkspaceItems_ThrowsBusinessRuleException()
    {
        var sourceItem = CreateBoardItem();
        var otherWorkspaceId = Guid.CreateVersion7();
        var targetItem = BoardItem.CreateRoot(
            TestAccountId,
            otherWorkspaceId,
            sourceItem.BoardId,
            sourceItem.GroupId,
            "Target Item",
            FractionalIndex.Create("a1"),
            TestUserId,
            TestNow);
        SetupBoardItems(sourceItem, targetItem);

        var command = new CreateBoardItemLinkCommand(sourceItem.Id, targetItem.Id, "related");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Handle_InvalidRelation_ThrowsBusinessRuleException()
    {
        var sourceItem = CreateBoardItem();
        var targetItem = CreateBoardItem(boardId: sourceItem.BoardId);
        SetupBoardItems(sourceItem, targetItem);

        var command = new CreateBoardItemLinkCommand(sourceItem.Id, targetItem.Id, "InvalidType");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Handle_BlockedBy_StoresADependencyFromTheViewedItemToTheOther()
    {
        var item = CreateBoardItem();
        var other = CreateBoardItem(boardId: item.BoardId);
        SetupBoardItems(item, other);
        var added = new List<BoardItemLink>();
        SetupBoardItemLinks();
        DbContextMock.Setup(c => c.BoardItemLinks).Returns(CreateAsyncDbSet(added));

        await _handler.Handle(new CreateBoardItemLinkCommand(item.Id, other.Id, "blocked-by"), CancellationToken.None);

        var link = added.Should().ContainSingle().Subject;
        link.SourceItemId.Should().Be(item.Id);
        link.Target.ResourceId.Should().Be(other.Id);
        link.LinkType.Should().Be(BoardItemLinkType.Dependency);
    }

    [Fact]
    public async Task Handle_Blocks_StoresTheDependencyTheOtherWayRound()
    {
        var item = CreateBoardItem();
        var other = CreateBoardItem(boardId: item.BoardId);
        SetupBoardItems(item, other);
        var added = new List<BoardItemLink>();
        SetupBoardItemLinks();
        DbContextMock.Setup(c => c.BoardItemLinks).Returns(CreateAsyncDbSet(added));

        await _handler.Handle(new CreateBoardItemLinkCommand(item.Id, other.Id, "blocks"), CancellationToken.None);

        var link = added.Should().ContainSingle().Subject;
        link.SourceItemId.Should().Be(other.Id);
        link.Target.ResourceId.Should().Be(item.Id);
        link.LinkType.Should().Be(BoardItemLinkType.Dependency);
    }

    [Fact]
    public async Task Handle_LinkToItself_ThrowsBusinessRuleException()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);

        await _handler.Invoking(h => h.Handle(new CreateBoardItemLinkCommand(item.Id, item.Id, "related"), CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Handle_SameRelationAgain_ReturnsTheExistingLinkAndAddsNone()
    {
        var item = CreateBoardItem();
        var other = CreateBoardItem(boardId: item.BoardId);
        SetupBoardItems(item, other);
        var existing = CreateBoardItemLink(sourceItemId: item.Id, targetItemId: other.Id);
        SetupBoardItemLinks(existing);

        var result = await _handler.Handle(new CreateBoardItemLinkCommand(item.Id, other.Id, "related"), CancellationToken.None);

        result.Data.Should().Be(existing.Id);
        DbContextMock.Object.BoardItemLinks.Should().ContainSingle();
    }
}
