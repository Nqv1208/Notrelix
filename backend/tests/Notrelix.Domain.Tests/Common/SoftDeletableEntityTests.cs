using FluentAssertions;

namespace Notrelix.Domain.Tests.Common;

public class SoftDeletableEntityTests
{
    private class TestEntity : SoftDeletableEntity
    {
        public void PublicApplyDeletion(SoftDeletableEntity.PendingDeletion deletion)
            => ApplyDeletion(deletion);

        public void PublicApplyRestore(SoftDeletableEntity.PendingRestore restore)
            => ApplyRestore(restore);

        public void PublicEnsureNotDeleted() => EnsureNotDeleted();
    }

    [Fact]
    public void PrepareDeletion_ShouldNotMutateState()
    {
        var entity = new TestEntity();
        var actorId = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;

        var pending = entity.PrepareDeletion(actorId, time, "Cleanup");

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
        pending.Reason.Should().Be("Cleanup");
        pending.ActorId.Should().Be(actorId);
        pending.OccurredAt.Should().Be(time);
    }

    [Fact]
    public void PrepareDeletion_InvalidTimestamp_ShouldThrow()
    {
        var entity = new TestEntity();
        var act = () => entity.PrepareDeletion(Guid.NewGuid(), default, null);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void PrepareDeletion_ShouldNormalizeReason()
    {
        var entity = new TestEntity();
        var pending = entity.PrepareDeletion(Guid.NewGuid(), DateTimeOffset.UtcNow, "  Cleanup  ");
        pending.Reason.Should().Be("Cleanup");
    }

    [Fact]
    public void PrepareDeletion_NullReason_ShouldStayNull()
    {
        var entity = new TestEntity();
        var pending = entity.PrepareDeletion(Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        pending.Reason.Should().BeNull();
    }

    [Fact]
    public void ApplyDeletion_ShouldSetDeletedProperties()
    {
        var entity = new TestEntity();
        var deletedBy = Guid.NewGuid();
        var deletedAt = DateTimeOffset.UtcNow;

        var pending = entity.PrepareDeletion(deletedBy, deletedAt, "Cleanup");
        entity.PublicApplyDeletion(pending);

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(deletedAt);
        entity.DeletedBy.Should().Be(deletedBy);
        entity.DeleteReason.Should().Be("Cleanup");
    }

    [Fact]
    public void PrepareRestore_ShouldNotMutateState()
    {
        var entity = new TestEntity();
        var userId = Guid.NewGuid();
        var deleteTime = DateTimeOffset.UtcNow;
        var restoreTime = deleteTime.AddMinutes(5);

        var del = entity.PrepareDeletion(userId, deleteTime, null);
        entity.PublicApplyDeletion(del);

        var pendingRestore = entity.PrepareRestore(userId, restoreTime);
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(deleteTime);
        pendingRestore.ActorId.Should().Be(userId);
        pendingRestore.OccurredAt.Should().Be(restoreTime);
    }

    [Fact]
    public void PrepareRestore_InvalidTimestamp_ShouldThrow()
    {
        var entity = new TestEntity();
        var act = () => entity.PrepareRestore(Guid.NewGuid(), default);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ApplyRestore_ShouldClearDeletedProperties()
    {
        var entity = new TestEntity();
        var userId = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;

        var del = entity.PrepareDeletion(userId, time, null);
        entity.PublicApplyDeletion(del);

        var restore = entity.PrepareRestore(userId, time.AddMinutes(1));
        entity.PublicApplyRestore(restore);

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
        entity.DeleteReason.Should().BeNull();
        entity.RestoredAt.Should().Be(time.AddMinutes(1));
        entity.RestoredBy.Should().Be(userId);
    }

    [Fact]
    public void EnsureNotDeleted_ShouldThrow_WhenDeleted()
    {
        var entity = new TestEntity();
        var pending = entity.PrepareDeletion(Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        entity.PublicApplyDeletion(pending);

        var act = () => entity.PublicEnsureNotDeleted();
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void EnsureNotDeleted_ShouldNotThrow_WhenNotDeleted()
    {
        var entity = new TestEntity();
        var act = () => entity.PublicEnsureNotDeleted();
        act.Should().NotThrow();
    }
}
