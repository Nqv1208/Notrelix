using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.ArchiveBoardView;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.RestoreBoardView;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.UnarchiveBoardView;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardViews;

/// <summary>Archive, unarchive and restore of a saved view: the version moves once, a repeat bumps nothing, another member's private view is the identical NotFound, and the default view hands over in the same write.</summary>
public class BoardViewArchiveTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Guid _board = Guid.CreateVersion7();

    public BoardViewArchiveTests() => _currentUser.Setup(c => c.UserId).Returns(TestUserId);

    private BoardView View(string name, bool isDefault = false, BoardViewVisibility visibility = BoardViewVisibility.Board, Guid? createdBy = null) =>
        BoardView.Create(
            TestAccountId, TestWorkspaceId, _board, name, ViewType.Table, BoardViewConfig.Create(JsonValue.EmptyObject()),
            createdBy ?? TestUserId, TestNow, isDefault: isDefault, visibility: visibility);

    private Task<Result> Archive(Guid id, Guid? next = null) =>
        new ArchiveBoardViewCommandHandler(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object)
            .Handle(new ArchiveBoardViewCommand(_board, id, 1, next), CancellationToken.None);

    private Task<Result> Unarchive(Guid id) =>
        new UnarchiveBoardViewCommandHandler(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object)
            .Handle(new UnarchiveBoardViewCommand(_board, id, 1), CancellationToken.None);

    [Fact]
    public async Task Archiving_TakesTheViewOutOnce_AndARepeatBumpsNothing()
    {
        var view = View("A");
        SetupBoardViews(view);
        var before = view.Version;

        (await Archive(view.Id)).Succeeded.Should().BeTrue();
        view.IsArchived.Should().BeTrue();
        view.Version.Should().Be(before + 1);

        await Archive(view.Id);
        view.Version.Should().Be(before + 1, "archiving an archived view is a no-op");
    }

    [Fact]
    public async Task Archiving_TheDefaultView_HandsTheDefaultToThePickedSharedView_InTheSameWrite()
    {
        var leaving = View("Default", isDefault: true);
        var oldest = View("Oldest");
        var picked = View("Picked");
        SetupBoardViews(leaving, oldest, picked);

        await Archive(leaving.Id, picked.Id);

        leaving.IsDefault.Should().BeFalse();
        picked.IsDefault.Should().BeTrue();
        oldest.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task Archiving_TheDefaultView_WithoutAPick_HandsItToTheOldestSharedView()
    {
        var leaving = View("Default", isDefault: true);
        var oldest = View("Oldest");
        SetupBoardViews(leaving, oldest);

        await Archive(leaving.Id);

        oldest.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Archiving_WithAPrivateViewAsTheSuccessor_IsNotFound_AndNothingChanges()
    {
        var leaving = View("Default", isDefault: true);
        var mine = View("Mine", visibility: BoardViewVisibility.Private);
        SetupBoardViews(leaving, mine);
        var before = leaving.Version;

        var act = () => Archive(leaving.Id, mine.Id);

        await act.Should().ThrowAsync<NotFoundException>();
        leaving.IsArchived.Should().BeFalse();
        leaving.Version.Should().Be(before);
    }

    [Fact]
    public async Task AnotherMembersPrivateView_IsTheSameNotFoundAsAMissingOne_ForEveryAction()
    {
        var theirs = View("Theirs", visibility: BoardViewVisibility.Private, createdBy: Guid.CreateVersion7());
        SetupBoardViews(theirs);
        var before = theirs.Version;

        var archive = () => Archive(theirs.Id);
        var unarchive = () => Unarchive(theirs.Id);
        var missing = () => Archive(Guid.CreateVersion7());

        var expected = (await missing.Should().ThrowAsync<NotFoundException>()).Which.Message.Replace(theirs.Id.ToString(), "<id>");
        foreach (var act in new[] { archive, unarchive })
            (await act.Should().ThrowAsync<NotFoundException>()).Which.GetType().Should().Be<NotFoundException>();
        expected.Should().Contain("BoardView");
        theirs.IsArchived.Should().BeFalse();
        theirs.Version.Should().Be(before);
    }

    [Fact]
    public async Task Unarchiving_BringsTheViewBack_AndALiveViewIsANoOp()
    {
        var view = View("A");
        SetupBoardViews(view);
        await Archive(view.Id);
        var archivedVersion = view.Version;

        await Unarchive(view.Id);
        view.IsArchived.Should().BeFalse();
        view.Version.Should().Be(archivedVersion + 1);

        await Unarchive(view.Id);
        view.Version.Should().Be(archivedVersion + 1, "unarchiving a live view is a no-op");
    }

    [Fact]
    public async Task Unarchiving_AnUnknownView_IsNotFound()
    {
        var act = () => Unarchive(Guid.CreateVersion7());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Restoring_ADeletedView_BringsItBackOnce_AndALiveViewIsANoOp()
    {
        var view = View("Gone");
        view.Delete(TestUserId, TestNow);
        SetupBoardViews(view);
        var handler = new RestoreBoardViewCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);
        var deletedVersion = view.Version;

        await handler.Handle(new RestoreBoardViewCommand(view.Id, deletedVersion), CancellationToken.None);
        view.IsDeleted.Should().BeFalse();
        view.Version.Should().Be(deletedVersion + 1);

        await handler.Handle(new RestoreBoardViewCommand(view.Id, deletedVersion + 1), CancellationToken.None);
        view.Version.Should().Be(deletedVersion + 1, "restoring a live view is a no-op");
    }

    [Fact]
    public async Task Restoring_AnotherMembersPrivateView_IsNotFound_AndItStaysDeleted()
    {
        var other = Guid.CreateVersion7();
        var theirs = View("Theirs", visibility: BoardViewVisibility.Private, createdBy: other);
        theirs.Delete(other, TestNow);
        SetupBoardViews(theirs);
        var handler = new RestoreBoardViewCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

        var act = () => handler.Handle(new RestoreBoardViewCommand(theirs.Id, theirs.Version), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        theirs.IsDeleted.Should().BeTrue();
    }
}
