using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.RenameBoardView;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SetBoardViewVisibility;
using Notrelix.Application.Features.WorkManagement.BoardViews.Queries.ListBoardViews;
using NotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardViews;

/// <summary>WM-8: a Board view is shared with the Board's members; a private view is its creator's alone.</summary>
public class BoardViewVisibilityTests : WorkManagementHandlerTestBase
{
    private static readonly Guid Colleague = Guid.CreateVersion7();

    private BoardView ViewBy(Guid creator, string name, BoardViewVisibility visibility, Guid boardId, bool isDefault = false) =>
        BoardView.Create(
            TestAccountId, TestWorkspaceId, boardId, name, ViewType.Table,
            BoardViewConfig.Create(JsonValue.Create("{}")), creator, TestNow, isDefault, visibility);

    [Fact]
    public async Task List_ShowsBoardViewsAndTheCallersOwnPrivateViewsOnly()
    {
        var boardId = Guid.CreateVersion7();
        var shared = ViewBy(Colleague, "Shared", BoardViewVisibility.Board, boardId);
        var mine = ViewBy(TestUserId, "Mine", BoardViewVisibility.Private, boardId);
        var theirs = ViewBy(Colleague, "Theirs", BoardViewVisibility.Private, boardId);
        var otherBoard = ViewBy(TestUserId, "Elsewhere", BoardViewVisibility.Board, Guid.CreateVersion7());
        SetupBoardViews(shared, mine, theirs, otherBoard);

        var result = await new ListBoardViewsQueryHandler(DbContextMock.Object, RequestContextMock.Object)
            .Handle(new ListBoardViewsQuery(boardId), CancellationToken.None);

        result.Data!.Select(v => v.Name).Should().BeEquivalentTo(["Shared", "Mine"]);
        result.Data!.Single(v => v.Name == "Mine").Visibility.Should().Be("Private");
        result.Data!.Single(v => v.Name == "Shared").CreatedBy.Should().Be(Colleague);
    }

    [Fact]
    public async Task List_PutsTheDefaultFirst()
    {
        var boardId = Guid.CreateVersion7();
        SetupBoardViews(
            ViewBy(TestUserId, "Plain", BoardViewVisibility.Board, boardId),
            ViewBy(TestUserId, "Home", BoardViewVisibility.Board, boardId, isDefault: true));

        var result = await new ListBoardViewsQueryHandler(DbContextMock.Object, RequestContextMock.Object)
            .Handle(new ListBoardViewsQuery(boardId), CancellationToken.None);

        result.Data!.Select(v => v.Name).Should().Equal("Home", "Plain");
    }

    [Fact]
    public async Task Rename_AnotherMembersPrivateView_IsNotFound()
    {
        var boardId = Guid.CreateVersion7();
        var theirs = ViewBy(Colleague, "Theirs", BoardViewVisibility.Private, boardId);
        SetupBoardViews(theirs);

        var handler = new RenameBoardViewCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

        await handler.Invoking(h => h.Handle(new RenameBoardViewCommand(theirs.Id, "Mine now", ExpectedVersion: 1), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
        theirs.Name.Should().Be("Theirs");
    }

    [Fact]
    public async Task Rename_ABoardViewMadeBySomeoneElse_IsAllowedForAnEditor()
    {
        var boardId = Guid.CreateVersion7();
        var shared = ViewBy(Colleague, "Shared", BoardViewVisibility.Board, boardId);
        SetupBoardViews(shared);

        var handler = new RenameBoardViewCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);
        await handler.Handle(new RenameBoardViewCommand(shared.Id, "Renamed", ExpectedVersion: 1), CancellationToken.None);

        shared.Name.Should().Be("Renamed");
    }

    [Fact]
    public async Task SetVisibility_TheCreatorSharesAPrivateView()
    {
        var boardId = Guid.CreateVersion7();
        var mine = ViewBy(TestUserId, "Mine", BoardViewVisibility.Private, boardId);
        SetupBoardViews(mine);

        var result = await Handler().Handle(new SetBoardViewVisibilityCommand(boardId, mine.Id, "board", ExpectedVersion: 1), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        mine.Visibility.Should().Be(BoardViewVisibility.Board);
    }

    [Fact]
    public async Task SetVisibility_ABoardViewSomeoneElseMade_IsRefused()
    {
        var boardId = Guid.CreateVersion7();
        var shared = ViewBy(Colleague, "Shared", BoardViewVisibility.Board, boardId);
        SetupBoardViews(shared);

        var result = await Handler().Handle(new SetBoardViewVisibilityCommand(boardId, shared.Id, "Private", ExpectedVersion: 1), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        shared.Visibility.Should().Be(BoardViewVisibility.Board);
    }

    [Fact]
    public async Task SetVisibility_AnotherMembersPrivateView_IsNotFound()
    {
        var boardId = Guid.CreateVersion7();
        var theirs = ViewBy(Colleague, "Theirs", BoardViewVisibility.Private, boardId);
        SetupBoardViews(theirs);

        await Handler().Invoking(h => h.Handle(new SetBoardViewVisibilityCommand(boardId, theirs.Id, "Board", ExpectedVersion: 1), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    private SetBoardViewVisibilityCommandHandler Handler() =>
        new(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);
}
