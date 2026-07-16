using Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.CreateBoardItemLink;

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

        var command = new CreateBoardItemLinkCommand(sourceItem.Id, targetItem.Id, "Reference");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_SourceItemNotFound_ThrowsNotFoundException()
    {
        var targetItem = CreateBoardItem();
        SetupBoardItems(targetItem);

        var command = new CreateBoardItemLinkCommand(Guid.CreateVersion7(), targetItem.Id, "Reference");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_TargetItemNotFound_ThrowsNotFoundException()
    {
        var sourceItem = CreateBoardItem();
        SetupBoardItems(sourceItem);

        var command = new CreateBoardItemLinkCommand(sourceItem.Id, Guid.CreateVersion7(), "Reference");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DifferentWorkspaceItems_ThrowsBusinessRuleException()
    {
        var sourceItem = CreateBoardItem();
        var otherWorkspaceId = Guid.CreateVersion7();
        var targetItem = BoardItem.Create(
            TestAccountId,
            otherWorkspaceId,
            sourceItem.BoardId,
            sourceItem.GroupId,
            "Target Item",
            FractionalIndex.Create("a1"),
            TestUserId,
            TestNow);
        SetupBoardItems(sourceItem, targetItem);

        var command = new CreateBoardItemLinkCommand(sourceItem.Id, targetItem.Id, "Reference");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Handle_InvalidLinkType_ThrowsBusinessRuleException()
    {
        var sourceItem = CreateBoardItem();
        var targetItem = CreateBoardItem(boardId: sourceItem.BoardId);
        SetupBoardItems(sourceItem, targetItem);

        var command = new CreateBoardItemLinkCommand(sourceItem.Id, targetItem.Id, "InvalidType");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>();
    }
}
