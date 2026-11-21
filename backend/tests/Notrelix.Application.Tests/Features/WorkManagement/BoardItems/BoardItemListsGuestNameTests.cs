using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListArchivedBoardItems;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemTrash;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

/// <summary>
/// The Trash and the Archive name who deleted or archived each item, behind the same rule as every change notice
/// (<c>ActorNotice</c>): a member reads the name, a Workspace Guest with Editor access reads the neutral copy.
/// </summary>
public sealed class BoardItemListsGuestNameTests : WorkManagementHandlerTestBase
{
    private const string ActorName = "Ada Lovelace";
    private readonly Guid _actor = Guid.CreateVersion7();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<IWorkspaceAccessResolver> _access = new();
    private readonly Board _board;

    public BoardItemListsGuestNameTests()
    {
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ActorSnapshot(_actor, ActorName, null)]);
        _board = CreateBoard();
        DateTimeProviderMock.Setup(c => c.UtcNow).Returns(TestNow.AddDays(2));
        var deleted = CreateBoardItem(boardId: _board.Id);
        deleted.Delete(_actor, TestNow.AddDays(1));
        var archived = CreateBoardItem(boardId: _board.Id);
        archived.Archive(_actor, TestNow.AddDays(1));
        SetupBoardItems(deleted, archived);
    }

    private void Viewer(bool guest) =>
        _access.Setup(a => a.ResolveAsync(TestWorkspaceId, TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceAccessSnapshot(TestAccountId, TestWorkspaceId, TestUserId, true, true, guest));

    private Task<BoardItemTrashPageDto> Trash() =>
        new ListBoardItemTrashQueryHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, _actors.Object, _access.Object)
            .Handle(new ListBoardItemTrashQuery(_board.Id), CancellationToken.None);

    private Task<ArchivedBoardItemPageDto> Archive() =>
        new ListArchivedBoardItemsQueryHandler(DbContextMock.Object, _actors.Object, RequestContextMock.Object, _access.Object)
            .Handle(new ListArchivedBoardItemsQuery(_board.Id), CancellationToken.None);

    [Fact]
    public async Task Trash_NamesTheDeleterToAMember()
    {
        Viewer(guest: false);
        var item = (await Trash()).Items.Should().ContainSingle().Which;
        item.DeletedBy.Should().Be(_actor);
        item.DeletedByName.Should().Be(ActorName);
    }

    [Fact]
    public async Task Trash_NamesNoOneToAGuest()
    {
        Viewer(guest: true);
        (await Trash()).Items.Should().ContainSingle().Which.DeletedByName.Should().BeNull();
    }

    [Fact]
    public async Task Archive_NamesTheArchiverToAMember()
    {
        Viewer(guest: false);
        var item = (await Archive()).Items.Should().ContainSingle().Which;
        item.ArchivedBy.Should().Be(_actor);
        item.ArchivedByName.Should().Be(ActorName);
    }

    [Fact]
    public async Task Archive_NamesNoOneToAGuest()
    {
        Viewer(guest: true);
        (await Archive()).Items.Should().ContainSingle().Which.ArchivedByName.Should().BeNull();
    }
}
