using FluentAssertions;

namespace Notrelix.Domain.Tests.Common;

public class DeleteTwoPhaseTests
{
    private class TestAggregateRoot : SoftDeletableAggregateRoot
    {
        public string? Name { get; private set; }

        public void SetName(string name) => Name = name;

        public PendingDeletion PublicPrepareDeletion(Guid? actorId, DateTimeOffset occurredAt, string? reason)
            => PrepareDeletion(actorId, occurredAt, reason);

        public void PublicApplyDeletion(PendingDeletion deletion)
            => ApplyDeletion(deletion);

        public PendingRestore PublicPrepareRestore(Guid? actorId, DateTimeOffset occurredAt)
            => PrepareRestore(actorId, occurredAt);

        public void PublicApplyRestore(PendingRestore restore)
            => ApplyRestore(restore);
    }

    private class TestChildEntity : SoftDeletableEntity
    {
        public string? Value { get; private set; }

        public void SetValue(string value) => Value = value;

        public PendingDeletion PublicPrepareDeletion(Guid? actorId, DateTimeOffset occurredAt, string? reason)
            => PrepareDeletion(actorId, occurredAt, reason);

        public void PublicApplyDeletion(PendingDeletion deletion)
            => ApplyDeletion(deletion);

        public PendingRestore PublicPrepareRestore(Guid? actorId, DateTimeOffset occurredAt)
            => PrepareRestore(actorId, occurredAt);

        public void PublicApplyRestore(PendingRestore restore)
            => ApplyRestore(restore);
    }

    [Fact]
    public void PrepareDeletion_AggregateRoot_ShouldNotMutateState()
    {
        var entity = new TestAggregateRoot();
        var actorId = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;

        var pending = entity.PublicPrepareDeletion(actorId, time, "Reason");

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

        var pending = entity.PublicPrepareDeletion(actorId, time, "Reason");

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        pending.ActorId.Should().Be(actorId);
    }

    [Fact]
    public void PrepareDeletion_InvalidTimestamp_ShouldThrow()
    {
        var aggregate = new TestAggregateRoot();
        var child = new TestChildEntity();

        var act1 = () => aggregate.PublicPrepareDeletion(Guid.NewGuid(), default, null);
        var act2 = () => child.PublicPrepareDeletion(Guid.NewGuid(), default, null);

        act1.Should().Throw<BusinessRuleException>();
        act2.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void PrepareDeletion_MinValueTimestamp_ShouldThrow()
    {
        var entity = new TestAggregateRoot();
        var act = () => entity.PublicPrepareDeletion(Guid.NewGuid(), DateTimeOffset.MinValue, null);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void PrepareDeletion_ShouldNormalizeReason()
    {
        var entity = new TestAggregateRoot();

        var pending = entity.PublicPrepareDeletion(Guid.NewGuid(), DateTimeOffset.UtcNow, "  reason  ");
        pending.Reason.Should().Be("reason");
    }

    [Fact]
    public void PrepareDeletion_NullReason_ShouldStayNull()
    {
        var entity = new TestAggregateRoot();
        var pending = entity.PublicPrepareDeletion(Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        pending.Reason.Should().BeNull();
    }

    [Fact]
    public void PrepareDeletion_EmptyReason_ShouldStayNull()
    {
        var entity = new TestAggregateRoot();
        var pending = entity.PublicPrepareDeletion(Guid.NewGuid(), DateTimeOffset.UtcNow, "   ");
        pending.Reason.Should().BeNull();
    }

    [Fact]
    public void ApplyDeletion_AggregateRoot_ShouldSetAllFields()
    {
        var entity = new TestAggregateRoot();
        var actorId = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;

        var pending = entity.PublicPrepareDeletion(actorId, time, "Cleanup");
        entity.PublicApplyDeletion(pending);

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

        var pending = entity.PublicPrepareDeletion(actorId, time, "Cleanup");
        entity.PublicApplyDeletion(pending);

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

        var pending = entity.PublicPrepareDeletion(actorId, time, null);
        entity.PublicApplyDeletion(pending);

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

        var del = entity.PublicPrepareDeletion(actorId, deleteTime, null);
        entity.PublicApplyDeletion(del);

        var pendingRestore = entity.PublicPrepareRestore(actorId, restoreTime);
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(deleteTime);
        pendingRestore.ActorId.Should().Be(actorId);
        pendingRestore.OccurredAt.Should().Be(restoreTime);
    }

    [Fact]
    public void PrepareRestore_InvalidTimestamp_ShouldThrow()
    {
        var entity = new TestAggregateRoot();
        var del = entity.PublicPrepareDeletion(Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        entity.PublicApplyDeletion(del);

        var act = () => entity.PublicPrepareRestore(Guid.NewGuid(), default);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ApplyRestore_ShouldClearDeletionFieldsAndApplyAudit()
    {
        var entity = new TestAggregateRoot();
        var actorId = Guid.NewGuid();
        var deleteTime = DateTimeOffset.UtcNow;
        var restoreTime = deleteTime.AddMinutes(5);

        var del = entity.PublicPrepareDeletion(actorId, deleteTime, "old reason");
        entity.PublicApplyDeletion(del);

        var restore = entity.PublicPrepareRestore(actorId, restoreTime);
        entity.PublicApplyRestore(restore);

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
        entity.DeleteReason.Should().BeNull();
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

        var del = entity.PublicPrepareDeletion(actorId, deleteTime, null);
        entity.PublicApplyDeletion(del);

        var restore = entity.PublicPrepareRestore(actorId, restoreTime);
        entity.PublicApplyRestore(restore);

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
        entity.DeleteReason.Should().BeNull();
    }

    [Fact]
    public void FailureAtomicity_PrepareDeletion_DoesNotCorruptState()
    {
        var entity = new TestAggregateRoot();
        var time = DateTimeOffset.UtcNow;

        var act = () => entity.PublicPrepareDeletion(Guid.NewGuid(), default, null);
        act.Should().Throw<BusinessRuleException>();

        entity.IsDeleted.Should().BeFalse();
        entity.CreatedAt.Should().Be(default);
        entity.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void FailureAtomicity_PrepareRestore_DoesNotCorruptState()
    {
        var entity = new TestAggregateRoot();
        var del = entity.PublicPrepareDeletion(Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        entity.PublicApplyDeletion(del);

        var act = () => entity.PublicPrepareRestore(Guid.NewGuid(), default);
        act.Should().Throw<BusinessRuleException>();

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().NotBeNull();
    }
}
