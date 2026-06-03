using Notrelix.Domain.Entities.Boards;
using Notrelix.Domain.Enums;
using Notrelix.Domain.Events.Board;

namespace Notrelix.Domain.Tests;

public class BoardBusinessRulesTests
{
    [Fact]
    public void BoardCreate_WhenValid_ShouldRaiseBoardCreatedEvent()
    {
        var workspaceId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();

        var board = Board.Create(workspaceId, creatorId, "Product Roadmap", "Delivery board");

        board.DomainEvents.Should().ContainSingle().Which
            .Should().BeOfType<BoardCreatedEvent>()
            .Which.BoardId.Should().Be(board.Id);
    }

    [Fact]
    public void BoardRename_WhenTitleChanges_ShouldRaiseBoardUpdatedEvent()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), "Old", "Board");
        board.ClearDomainEvents();
        var actorId = Guid.NewGuid();

        board.Rename("New name", actorId);

        board.Title.Should().Be("New name");
        var domainEvent = board.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BoardUpdatedEvent>().Subject;
        domainEvent.BoardId.Should().Be(board.Id);
        domainEvent.UpdatedBy.Should().Be(actorId);
    }

    [Fact]
    public void BoardArchive_WhenActive_ShouldRaiseBoardArchivedEvent()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), "Board", null);
        board.ClearDomainEvents();
        var actorId = Guid.NewGuid();

        board.Archive(actorId);

        board.IsArchived.Should().BeTrue();
        board.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BoardArchivedEvent>()
            .Which.ArchivedBy.Should().Be(actorId);
    }

    [Fact]
    public void BoardListBehavior_ShouldRaiseGroupEventsForImportantChanges()
    {
        var actorId = Guid.NewGuid();
        var list = BoardList.Create(Guid.NewGuid(), "Backlog", 1024, "#579bfc");

        list.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BoardGroupCreatedEvent>();

        list.ClearDomainEvents();
        list.Rename("Ready", actorId);
        list.ChangeColor("#00c875", actorId);
        list.Move(2048, actorId);
        list.Archive(actorId);

        list.Title.Should().Be("Ready");
        list.Color.Should().Be("#00c875");
        list.Position.Should().Be(2048);
        list.IsArchived.Should().BeTrue();
        list.DomainEvents.Select(e => e.GetType()).Should().ContainInOrder(
            typeof(BoardGroupUpdatedEvent),
            typeof(BoardGroupColorChangedEvent),
            typeof(BoardGroupReorderedEvent),
            typeof(BoardGroupDeletedEvent));
    }

    [Fact]
    public void BoardViewBehavior_ShouldSupportSavedViewDefaultAndReorderEvents()
    {
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var view = BoardView.CreateSaved(boardId, userId, "Main table", ViewMode.List, 1024, isDefault: true);

        view.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BoardViewCreatedEvent>();

        view.ClearDomainEvents();
        view.SetDefault(actorId);
        view.Move(2048, actorId);

        view.IsDefault.Should().BeTrue();
        view.Position.Should().Be(2048);
        view.DomainEvents.Select(e => e.GetType()).Should().ContainInOrder(
            typeof(BoardViewDefaultChangedEvent),
            typeof(BoardViewReorderedEvent));
    }
}
