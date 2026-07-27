using FluentAssertions;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardItemTests
{
    private BoardItem CreateValidItem()
    {
        return BoardItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Item 1", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
    }

    [Fact]
    public void UpdateFieldValue_ShouldAddValueAndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var item = BoardItem.Create(Guid.NewGuid(), workspaceId, boardId, groupId, "Item 1", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)item).ClearDomainEvents();

        var field = BoardField.Create(Guid.NewGuid(), workspaceId, boardId, "My Field", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var value = FieldValue.Create(JsonValue.Create("\"Hello\""));
        var updatedBy = Guid.NewGuid();

        item.UpdateFieldValue(field, value, updatedBy, DateTimeOffset.UtcNow);

        item.FieldValues.Should().HaveCount(1);
        item.FieldValues.First().Value.Should().Be(value);
        item.UpdatedBy.Should().Be(updatedBy);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemFieldValueChangedDomainEvent);
    }

    [Fact]
    public void UpdateFieldValue_ShouldThrow_WhenItemIsDeleted()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var item = BoardItem.Create(Guid.NewGuid(), workspaceId, boardId, groupId, "Item 1", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        item.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var field = BoardField.Create(Guid.NewGuid(), workspaceId, boardId, "My Field", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var value = FieldValue.Create(JsonValue.Create("\"Hello\""));

        Action act = () => item.UpdateFieldValue(field, value, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MoveToGroup_ShouldUpdateGroupAndPosition()
    {
        var item = CreateValidItem();
        ((IHasDomainEvents)item).ClearDomainEvents();

        var newGroup = Guid.NewGuid();
        var newPosition = FractionalIndex.Create("a1");
        var updatedBy = Guid.NewGuid();

        var boardGroupRef = new BoardGroupRef(Guid.NewGuid(), item.WorkspaceId, item.BoardId, newGroup);
        item.MoveToGroup(boardGroupRef, newPosition, updatedBy, DateTimeOffset.UtcNow);

        item.GroupId.Should().Be(newGroup);
        item.Position.Should().Be(newPosition);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemMovedDomainEvent);
    }

    [Fact]
    public void SoftDeleteAndRestore_ShouldWorkCorrectly()
    {
        var item = CreateValidItem();
        ((IHasDomainEvents)item).ClearDomainEvents();

        item.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        item.IsDeleted.Should().BeTrue();
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemSoftDeletedDomainEvent);

        ((IHasDomainEvents)item).ClearDomainEvents();
        item.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);
        item.IsDeleted.Should().BeFalse();
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemRestoredDomainEvent);
    }

    [Fact]
    public void UpdateFieldValue_ShouldThrow_WhenFieldFromDifferentWorkspace()
    {
        var item = BoardItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)item).ClearDomainEvents();

        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), item.BoardId, "Field", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var value = FieldValue.Create(JsonValue.Create("\"Hello\""));

        Action act = () => item.UpdateFieldValue(field, value, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*workspace*");
    }

    [Fact]
    public void UpdateFieldValue_ShouldThrow_WhenFieldFromDifferentBoard()
    {
        var item = BoardItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)item).ClearDomainEvents();

        var field = BoardField.Create(Guid.NewGuid(), item.WorkspaceId, Guid.NewGuid(), "Field", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var value = FieldValue.Create(JsonValue.Create("\"Hello\""));

        Action act = () => item.UpdateFieldValue(field, value, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*board*");
    }

    [Fact]
    public void UpdateFieldValue_ShouldThrow_WhenSelectValueNotInOptions()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var item = BoardItem.Create(Guid.NewGuid(), workspaceId, boardId, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)item).ClearDomainEvents();

        var field = BoardField.Create(Guid.NewGuid(), workspaceId, boardId, "Status", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var nonExistentOptionId = Guid.NewGuid().ToString();
        var value = FieldValue.Create(JsonValue.Create($"\"{nonExistentOptionId}\""));

        Action act = () => item.UpdateFieldValue(field, value, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*option*");
    }

    [Fact]
    public void UpdateFieldValue_ShouldAccept_WhenSelectValueMatchesOption()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var item = BoardItem.Create(Guid.NewGuid(), workspaceId, boardId, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)item).ClearDomainEvents();

        var field = BoardField.Create(Guid.NewGuid(), workspaceId, boardId, "Status", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var optionId = field.Options.First().Id.ToString();
        var value = FieldValue.Create(JsonValue.Create($"\"{optionId}\""));

        item.UpdateFieldValue(field, value, Guid.NewGuid(), DateTimeOffset.UtcNow);

        item.FieldValues.Should().HaveCount(1);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemFieldValueChangedDomainEvent);
    }

    [Fact]
    public void Archive_ShouldSetIsArchived_AndRaiseEvent()
    {
        var item = CreateValidItem();
        ((IHasDomainEvents)item).ClearDomainEvents();

        item.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        item.IsArchived.Should().BeTrue();
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemArchivedDomainEvent);
    }

    [Fact]
    public void Archive_ShouldBeIdempotent()
    {
        var item = CreateValidItem();
        item.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)item).ClearDomainEvents();

        item.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        item.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Archive_ShouldThrow_WhenDeleted()
    {
        var item = CreateValidItem();
        item.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => item.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Unarchive_ShouldClearIsArchived_AndRaiseEvent()
    {
        var item = CreateValidItem();
        item.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)item).ClearDomainEvents();

        item.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        item.IsArchived.Should().BeFalse();
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemUnarchivedDomainEvent);
    }

    [Fact]
    public void Unarchive_ShouldBeIdempotent()
    {
        var item = CreateValidItem();
        ((IHasDomainEvents)item).ClearDomainEvents();

        item.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        item.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Unarchive_ShouldThrow_WhenDeleted()
    {
        var item = CreateValidItem();
        item.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        item.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => item.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }
}
