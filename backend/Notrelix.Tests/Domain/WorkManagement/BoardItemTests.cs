using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Xunit;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardItemTests
{
    private BoardItem CreateValidItem()
    {
        return BoardItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Item 1", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
    }

    [Fact]
    public void UpdateFieldValue_ShouldAddValueAndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        
        var item = BoardItem.Create(workspaceId, boardId, groupId, "Item 1", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        item.ClearDomainEvents();
        
        var field = BoardField.Create(workspaceId, boardId, "My Field", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var value = FieldValue.Create(JsonValue.Create("\"Hello\""));
        var updatedBy = Guid.NewGuid();
        
        item.UpdateFieldValue(field, value, updatedBy, DateTimeOffset.UtcNow);
        
        item.FieldValues.Should().HaveCount(1);
        item.FieldValues.First().Value.Should().Be(value);
        item.UpdatedBy.Should().Be(updatedBy);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemFieldValueChangedEvent);
    }

    [Fact]
    public void UpdateFieldValue_ShouldThrow_WhenItemIsDeleted()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        
        var item = BoardItem.Create(workspaceId, boardId, groupId, "Item 1", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        item.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        
        var field = BoardField.Create(workspaceId, boardId, "My Field", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var value = FieldValue.Create(JsonValue.Create("\"Hello\""));
        
        Action act = () => item.UpdateFieldValue(field, value, Guid.NewGuid(), DateTimeOffset.UtcNow);
        
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MoveToGroup_ShouldUpdateGroupAndPosition()
    {
        var item = CreateValidItem();
        item.ClearDomainEvents();
        
        var newGroup = Guid.NewGuid();
        var newPosition = FractionalIndex.Create("b0");
        var updatedBy = Guid.NewGuid();
        
        var boardGroupRef = new BoardGroupRef(item.WorkspaceId, item.BoardId, newGroup);
        item.MoveToGroup(boardGroupRef, newPosition, updatedBy, DateTimeOffset.UtcNow);
        
        item.GroupId.Should().Be(newGroup);
        item.Position.Should().Be(newPosition);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemMovedEvent);
    }

    [Fact]
    public void SoftDeleteAndRestore_ShouldWorkCorrectly()
    {
        var item = CreateValidItem();
        item.ClearDomainEvents();
        
        item.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        item.IsDeleted.Should().BeTrue();
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemSoftDeletedEvent);
        
        item.ClearDomainEvents();
        item.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);
        item.IsDeleted.Should().BeFalse();
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemRestoredEvent);
    }

    [Fact]
    public void UpdateFieldValue_ShouldThrow_WhenFieldFromDifferentWorkspace()
    {
        var item = BoardItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        item.ClearDomainEvents();

        var field = BoardField.Create(Guid.NewGuid(), item.BoardId, "Field", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var value = FieldValue.Create(JsonValue.Create("\"Hello\""));

        Action act = () => item.UpdateFieldValue(field, value, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*workspace*");
    }

    [Fact]
    public void UpdateFieldValue_ShouldThrow_WhenFieldFromDifferentBoard()
    {
        var item = BoardItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        item.ClearDomainEvents();

        var field = BoardField.Create(item.WorkspaceId, Guid.NewGuid(), "Field", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var value = FieldValue.Create(JsonValue.Create("\"Hello\""));

        Action act = () => item.UpdateFieldValue(field, value, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*board*");
    }

    [Fact]
    public void UpdateFieldValue_ShouldThrow_WhenSelectValueNotInOptions()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var item = BoardItem.Create(workspaceId, boardId, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        item.ClearDomainEvents();

        var field = BoardField.Create(workspaceId, boardId, "Status", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("b0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
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
        var item = BoardItem.Create(workspaceId, boardId, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        item.ClearDomainEvents();

        var field = BoardField.Create(workspaceId, boardId, "Status", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("b0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var optionId = field.Options.First().Id.ToString();
        var value = FieldValue.Create(JsonValue.Create($"\"{optionId}\""));

        item.UpdateFieldValue(field, value, Guid.NewGuid(), DateTimeOffset.UtcNow);

        item.FieldValues.Should().HaveCount(1);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemFieldValueChangedEvent);
    }
}
