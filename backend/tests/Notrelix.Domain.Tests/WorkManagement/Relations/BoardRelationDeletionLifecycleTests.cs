using FluentAssertions;
using Notrelix.Domain.WorkManagement.Relations;

namespace Notrelix.Domain.Tests.WorkManagement.Relations;

/// <summary>
/// Tests for BoardRelation deletion lifecycle.
/// Invariant: Delete/Restore preserve BoardRelation.Status.
/// Business lifecycle: Active / Paused / Broken
/// Deletion availability: IsDeleted
/// </summary>
public class BoardRelationDeletionLifecycleTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid BoardB = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static BoardRelation CreateRelation(BoardRelationStatus targetStatus)
    {
        var relation = BoardRelation.Create(
            Guid.NewGuid(), WsA, BoardA, BoardB,
            null, null, Actor, Now);

        return targetStatus switch
        {
            BoardRelationStatus.Active => relation,
            BoardRelationStatus.Paused => PauseRelation(relation),
            BoardRelationStatus.Broken => BreakRelation(relation),
            _ => relation
        };
    }

    private static BoardRelation PauseRelation(BoardRelation relation)
    {
        relation.Pause(Actor, Now);
        return relation;
    }

    private static BoardRelation BreakRelation(BoardRelation relation)
    {
        relation.MarkBroken(Actor, Now);
        return relation;
    }

    // ── Delete preserves status ───────────────────────────────────────────

    [Fact]
    public void Delete_Active_PreservesActive()
    {
        var relation = CreateRelation(BoardRelationStatus.Active);

        relation.Delete(Actor, Now);

        relation.Status.Should().Be(BoardRelationStatus.Active);
        relation.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Delete_Paused_PreservesPaused()
    {
        var relation = CreateRelation(BoardRelationStatus.Paused);

        relation.Delete(Actor, Now);

        relation.Status.Should().Be(BoardRelationStatus.Paused);
        relation.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Delete_Broken_PreservesBroken()
    {
        var relation = CreateRelation(BoardRelationStatus.Broken);

        relation.Delete(Actor, Now);

        relation.Status.Should().Be(BoardRelationStatus.Broken);
        relation.IsDeleted.Should().BeTrue();
    }

    // ── Restore preserves status ──────────────────────────────────────────

    [Fact]
    public void Restore_Active_PreservesActive()
    {
        var relation = CreateRelation(BoardRelationStatus.Active);
        relation.Delete(Actor, Now);

        relation.Restore(Actor, Now);

        relation.Status.Should().Be(BoardRelationStatus.Active);
        relation.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Restore_Paused_PreservesPaused()
    {
        var relation = CreateRelation(BoardRelationStatus.Paused);
        relation.Delete(Actor, Now);

        relation.Restore(Actor, Now);

        relation.Status.Should().Be(BoardRelationStatus.Paused);
        relation.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Restore_Broken_PreservesBroken()
    {
        var relation = CreateRelation(BoardRelationStatus.Broken);
        relation.Delete(Actor, Now);

        relation.Restore(Actor, Now);

        relation.Status.Should().Be(BoardRelationStatus.Broken);
        relation.IsDeleted.Should().BeFalse();
    }

    // ── No-op behavior ────────────────────────────────────────────────────

    [Fact]
    public void Delete_AlreadyDeleted_IsNoOp()
    {
        var relation = CreateRelation(BoardRelationStatus.Active);
        relation.Delete(Actor, Now);
        ((IHasDomainEvents)relation).ClearDomainEvents();
        var versionBefore = relation.Version;

        relation.Delete(Actor, Now);

        relation.Version.Should().Be(versionBefore);
        relation.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_NotDeleted_IsNoOp()
    {
        var relation = CreateRelation(BoardRelationStatus.Active);
        ((IHasDomainEvents)relation).ClearDomainEvents();
        var versionBefore = relation.Version;

        relation.Restore(Actor, Now);

        relation.Version.Should().Be(versionBefore);
        relation.DomainEvents.Should().BeEmpty();
    }

    // ── Mutations rejected when deleted ───────────────────────────────────

    [Fact]
    public void Pause_OnDeleted_Rejected()
    {
        var relation = CreateRelation(BoardRelationStatus.Active);
        relation.Delete(Actor, Now);

        var act = () => relation.Pause(Actor, Now);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Resume_OnDeleted_Rejected()
    {
        var relation = CreateRelation(BoardRelationStatus.Paused);
        relation.Delete(Actor, Now);

        var act = () => relation.Resume(Actor, Now);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void MarkBroken_OnDeleted_Rejected()
    {
        var relation = CreateRelation(BoardRelationStatus.Active);
        relation.Delete(Actor, Now);

        var act = () => relation.MarkBroken(Actor, Now);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    // ── Event contracts ───────────────────────────────────────────────────

    [Fact]
    public void Delete_DoesNotEmitPausedOrBrokenEvent()
    {
        var relation = CreateRelation(BoardRelationStatus.Active);
        ((IHasDomainEvents)relation).ClearDomainEvents();

        relation.Delete(Actor, Now);

        relation.DomainEvents.Should().ContainSingle(e => e is BoardRelationDeletedDomainEvent);
        relation.DomainEvents.Should().NotContain(e => e is BoardRelationPausedDomainEvent);
        relation.DomainEvents.Should().NotContain(e => e is BoardRelationMarkedBrokenDomainEvent);
    }

    [Fact]
    public void Restore_DoesNotEmitResumedEvent()
    {
        var relation = CreateRelation(BoardRelationStatus.Active);
        relation.Delete(Actor, Now);
        ((IHasDomainEvents)relation).ClearDomainEvents();

        relation.Restore(Actor, Now);

        relation.DomainEvents.Should().ContainSingle(e => e is BoardRelationRestoredDomainEvent);
        relation.DomainEvents.Should().NotContain(e => e is BoardRelationResumedDomainEvent);
    }

    // ── Failure atomicity ─────────────────────────────────────────────────

    [Fact]
    public void Delete_WhenDeleted_ShouldBeFailureAtomic()
    {
        var relation = CreateRelation(BoardRelationStatus.Active);
        relation.Delete(Actor, Now);

        var statusBefore = relation.Status;
        var isDeletedBefore = relation.IsDeleted;
        var deletedAtBefore = relation.DeletedAt;
        var deletedByBefore = relation.DeletedBy;
        var deleteReasonBefore = relation.DeleteReason;
        var updatedAtBefore = relation.UpdatedAt;
        var updatedByBefore = relation.UpdatedBy;
        var versionBefore = relation.Version;
        var eventsBefore = relation.DomainEvents.Count;

        relation.Delete(Actor, Now);

        relation.Status.Should().Be(statusBefore);
        relation.IsDeleted.Should().Be(isDeletedBefore);
        relation.DeletedAt.Should().Be(deletedAtBefore);
        relation.DeletedBy.Should().Be(deletedByBefore);
        relation.DeleteReason.Should().Be(deleteReasonBefore);
        relation.UpdatedAt.Should().Be(updatedAtBefore);
        relation.UpdatedBy.Should().Be(updatedByBefore);
        relation.Version.Should().Be(versionBefore);
        relation.DomainEvents.Count.Should().Be(eventsBefore);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeFailureAtomic()
    {
        var relation = CreateRelation(BoardRelationStatus.Active);

        var statusBefore = relation.Status;
        var isDeletedBefore = relation.IsDeleted;
        var updatedAtBefore = relation.UpdatedAt;
        var updatedByBefore = relation.UpdatedBy;
        var versionBefore = relation.Version;
        var eventsBefore = relation.DomainEvents.Count;

        relation.Restore(Actor, Now);

        relation.Status.Should().Be(statusBefore);
        relation.IsDeleted.Should().Be(isDeletedBefore);
        relation.UpdatedAt.Should().Be(updatedAtBefore);
        relation.UpdatedBy.Should().Be(updatedByBefore);
        relation.Version.Should().Be(versionBefore);
        relation.DomainEvents.Count.Should().Be(eventsBefore);
    }
}
