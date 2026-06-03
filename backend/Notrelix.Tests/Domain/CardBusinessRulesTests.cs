using Notrelix.Domain.Entities.Boards;
using Notrelix.Domain.Enums;
using Notrelix.Domain.Events.Board;

namespace Notrelix.Domain.Tests;

public class CardBusinessRulesTests
{
    [Fact]
    public void CardCreate_WhenBoardContextProvided_ShouldRaiseCardCreatedEvent()
    {
        var boardId = Guid.NewGuid();
        var listId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();

        var card = Card.Create(listId, boardId, creatorId, "Launch task", 1024);

        card.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CardCreatedEvent>()
            .Which.BoardId.Should().Be(boardId);
    }

    [Fact]
    public void CardStateChanges_ShouldRaiseSpecificEventsWithOldAndNewValues()
    {
        var card = Card.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Task", 1024);
        card.ClearDomainEvents();
        var actorId = Guid.NewGuid();
        var dueDate = DateTime.UtcNow.AddDays(3);

        card.Rename("Renamed task", actorId);
        card.ChangeStatus(CardStatus.InProgress, actorId);
        card.ChangePriority(CardPriority.High, actorId);
        card.SetDueDate(dueDate, actorId);

        card.Title.Should().Be("Renamed task");
        card.Status.Should().Be(CardStatus.InProgress);
        card.Priority.Should().Be(CardPriority.High);
        card.DueDate.Should().Be(dueDate);
        card.DomainEvents.Select(e => e.GetType()).Should().ContainInOrder(
            typeof(CardUpdatedEvent),
            typeof(CardStatusChangedEvent),
            typeof(CardPriorityChangedEvent),
            typeof(CardDueDateChangedEvent));
    }

    [Fact]
    public void CardRelationshipAndLifecycleChanges_ShouldRaiseSpecificEvents()
    {
        var oldListId = Guid.NewGuid();
        var newListId = Guid.NewGuid();
        var pageId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var card = Card.Create(oldListId, Guid.NewGuid(), Guid.NewGuid(), "Task", 1024);
        card.ClearDomainEvents();

        card.MoveToGroup(newListId, 2048, actorId);
        card.LinkPage(pageId, actorId);
        card.UnlinkPage(actorId);
        card.Archive(actorId);
        card.SoftDelete(actorId);

        card.ListId.Should().Be(newListId);
        card.Position.Should().Be(2048);
        card.LinkedPageId.Should().BeNull();
        card.IsArchived.Should().BeTrue();
        card.IsDeleted.Should().BeTrue();
        card.DeletedAt.Should().NotBeNull();
        card.DomainEvents.Select(e => e.GetType()).Should().ContainInOrder(
            typeof(CardMovedEvent),
            typeof(CardLinkedToPageEvent),
            typeof(CardUnlinkedFromPageEvent),
            typeof(CardArchivedEvent),
            typeof(CardDeletedEvent));
    }
}
