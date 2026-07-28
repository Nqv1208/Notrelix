using FluentAssertions;

namespace Notrelix.Domain.Tests.Common;

public class SoftDeleteTwoPhaseTests
{
    private class TestAggregateRoot : SoftDeletableAggregateRoot
    {
        public string? Name { get; private set; }

        public void SetName(string name) => Name = name;
    }

    private class TestChildEntity : SoftDeletableEntity
    {
        public string? Value { get; private set; }

        public void SetValue(string value) => Value = value;
    }

    [Fact]
    public void PrepareDeletion_AggregateRoot_ShouldNotMutateState()
    {
        var entity = new TestAggregateRoot();
        var actorId = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;

        var pending = entity.PrepareDeletion(actorId, time, "Reason");

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
        entity.DeleteReason.Should().BeNull();
        pending.ActorId.Should().Be(actorId);
        pending.OccurredAt.Should().Be(time);
        pending.Reason.Should().Be("Reason");
    }

    [Fact]
    public void PrepareDeletion_ChildEntity_ShouldNotMutateState()
    {
        var entity = new TestChildEntity();
        var actorId = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;

        var pending = entity.PrepareDeletion(actorId, time, "Reason");

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        pending.ActorId.Should().Be(actorId);
    }

    [Fact]
    public void PrepareDeletion_InvalidTimestamp_ShouldThrow()
    {
        var aggregate = new TestAggregateRoot();
        var child = new TestChildEntity();

        var act1 = () => aggregate.PrepareDeletion(Guid.NewGuid(), default, null);
        var act2 = () => child.PrepareDeletion(Guid.NewGuid(), default, null);

        act1.Should().Throw<BusinessRuleException>();
        act2.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void PrepareDeletion_MinValueTimestamp_ShouldThrow()
    {
        var entity = new TestAggregateRoot();
        var act = () => entity.PrepareDeletion(Guid.NewGuid(), DateTimeOffset.MinValue, null);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void PrepareDeletion_ShouldNormalizeReason()
    {
        var entity = new TestAggregateRoot();

        var pending = entity.PrepareDeletion(Guid.NewGuid(), DateTimeOffset.UtcNow, "  reason  ");
        pending.Reason.Should().Be("reason");
    }

    [Fact]
    public void PrepareDeletion_NullReason_ShouldStayNull()
    {
        var entity = new TestAggregateRoot();
        var pending = entity.PrepareDeletion(Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        pending.Reason.Should().BeNull();
    }

    [Fact]
    public void PrepareDeletion_EmptyReason_ShouldStayNull()
    {
        var entity = new TestAggregateRoot();
        var pending = entity.PrepareDeletion(Guid.NewGuid(), DateTimeOffset.UtcNow, "   ");
        pending.Reason.Should().BeNull();
    }

    [Fact]
    public void ApplyDeletion_AggregateRoot_ShouldSetAllFields()
    {
        var entity = new TestAggregateRoot();
        var actorId = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;

        var pending = entity.PrepareDeletion(actorId, time, "Cleanup");
        entity.ApplyDeletion(pending);

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(time);
        entity.DeletedBy.Should().Be(actorId);
        entity.DeleteReason.Should().Be("Cleanup");
    }

    [Fact]
    public void ApplyDeletion_ChildEntity_ShouldSetAllFields()
    {
        var entity = new TestChildEntity();
        var actorId = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;

        var pending = entity.PrepareDeletion(actorId, time, "Cleanup");
        entity.ApplyDeletion(pending);

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(time);
        entity.DeletedBy.Should().Be(actorId);
        entity.DeleteReason.Should().Be("Cleanup");
    }

    [Fact]
    public void ApplyDeletion_ShouldAlsoApplyAudit()
    {
        var entity = new TestAggregateRoot();
        var actorId = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;

        var pending = entity.PrepareDeletion(actorId, time, null);
        entity.ApplyDeletion(pending);

        entity.UpdatedAt.Should().Be(time);
        entity.UpdatedBy.Should().Be(actorId);
    }

    [Fact]
    public void PrepareRestore_AggregateRoot_ShouldNotMutateState()
    {
        var entity = new TestAggregateRoot();
        var actorId = Guid.NewGuid();
        var deleteTime = DateTimeOffset.UtcNow;
        var restoreTime = deleteTime.AddMinutes(5);

        var del = entity.PrepareDeletion(actorId, deleteTime, null);
        entity.ApplyDeletion(del);

        var pendingRestore = entity.PrepareRestore(actorId, restoreTime);
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(deleteTime);
        pendingRestore.ActorId.Should().Be(actorId);
        pendingRestore.OccurredAt.Should().Be(restoreTime);
    }

    [Fact]
    public void PrepareRestore_InvalidTimestamp_ShouldThrow()
    {
        var entity = new TestAggregateRoot();
        var del = entity.PrepareDeletion(Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        entity.ApplyDeletion(del);

        var act = () => entity.PrepareRestore(Guid.NewGuid(), default);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ApplyRestore_ShouldClearDeletionFieldsAndApplyAudit()
    {
        var entity = new TestAggregateRoot();
        var actorId = Guid.NewGuid();
        var deleteTime = DateTimeOffset.UtcNow;
        var restoreTime = deleteTime.AddMinutes(5);

        var del = entity.PrepareDeletion(actorId, deleteTime, "old reason");
        entity.ApplyDeletion(del);

        var restore = entity.PrepareRestore(actorId, restoreTime);
        entity.ApplyRestore(restore);

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
        entity.DeleteReason.Should().BeNull();
        entity.RestoredAt.Should().Be(restoreTime);
        entity.RestoredBy.Should().Be(actorId);
        entity.UpdatedAt.Should().Be(restoreTime);
        entity.UpdatedBy.Should().Be(actorId);
    }

    [Fact]
    public void ApplyRestore_ChildEntity_ShouldClearDeletionFields()
    {
        var entity = new TestChildEntity();
        var actorId = Guid.NewGuid();
        var deleteTime = DateTimeOffset.UtcNow;
        var restoreTime = deleteTime.AddMinutes(5);

        var del = entity.PrepareDeletion(actorId, deleteTime, null);
        entity.ApplyDeletion(del);

        var restore = entity.PrepareRestore(actorId, restoreTime);
        entity.ApplyRestore(restore);

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.RestoredAt.Should().Be(restoreTime);
        entity.RestoredBy.Should().Be(actorId);
    }

    [Fact]
    public void FailureAtomicity_PrepareDeletion_DoesNotCorruptState()
    {
        var entity = new TestAggregateRoot();
        var time = DateTimeOffset.UtcNow;

        var act = () => entity.PrepareDeletion(Guid.NewGuid(), default, null);
        act.Should().Throw<BusinessRuleException>();

        entity.IsDeleted.Should().BeFalse();
        entity.CreatedAt.Should().Be(default);
        entity.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void FailureAtomicity_PrepareRestore_DoesNotCorruptState()
    {
        var entity = new TestAggregateRoot();
        var del = entity.PrepareDeletion(Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        entity.ApplyDeletion(del);

        var act = () => entity.PrepareRestore(Guid.NewGuid(), default);
        act.Should().Throw<BusinessRuleException>();

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().NotBeNull();
    }
}
