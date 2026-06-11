using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Items;
using Xunit;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardItemTests
{
    private BoardItem CreateValidItem()
    {
        return BoardItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Item 1", FractionalIndex.Create(1.0), Guid.NewGuid());
    }

    [Fact]
    public void UpdateFieldValue_ShouldAddValueAndRaiseEvent()
    {
        var item = CreateValidItem();
        item.ClearDomainEvents();
        
        var fieldId = Guid.NewGuid();
        var value = FieldValue.Create(JsonValue.Create("{\"text\":\"Hello\"}"));
        var updatedBy = Guid.NewGuid();
        
        item.UpdateFieldValue(fieldId, value, updatedBy);
        
        item.FieldValues.Should().HaveCount(1);
        item.FieldValues.First().Value.Should().Be(value);
        item.UpdatedBy.Should().Be(updatedBy);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemFieldValueChangedEvent);
    }

    [Fact]
    public void UpdateFieldValue_ShouldThrow_WhenItemIsDeleted()
    {
        var item = CreateValidItem();
        item.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        
        var value = FieldValue.Create(JsonValue.Create("{\"text\":\"Hello\"}"));
        
        Action act = () => item.UpdateFieldValue(Guid.NewGuid(), value, Guid.NewGuid());
        
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MoveToGroup_ShouldUpdateGroupAndPosition()
    {
        var item = CreateValidItem();
        item.ClearDomainEvents();
        
        var newGroup = Guid.NewGuid();
        var newPosition = FractionalIndex.Create(2.5);
        var updatedBy = Guid.NewGuid();
        
        item.MoveToGroup(newGroup, newPosition, updatedBy);
        
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
}
