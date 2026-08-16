using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity;

public class MfaRecoveryBatchTests
{
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly string[] ValidHashes = ["hash-a", "hash-b", "hash-c"];

    private static MfaRecoveryBatch CreateBatch(string[]? hashes = null, DateTimeOffset? createdAt = null)
    {
        return MfaRecoveryBatch.Create(
            Guid.NewGuid(),
            hashes ?? ValidHashes,
            createdAt ?? DateTimeOffset.UtcNow,
            ActorId);
    }

    [Fact]
    public void Create_ShouldSetPropertiesAndRaiseEvent()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var batch = MfaRecoveryBatch.Create(userId, ValidHashes, now, ActorId);

        batch.UserId.Should().Be(userId);
        batch.InvalidatedAt.Should().BeNull();
        batch.Codes.Should().HaveCount(3);
        batch.Codes.Select(c => c.CodeHash).Should().Equal(ValidHashes);
        batch.Codes.Should().OnlyContain(c => c.ConsumedAt == null);
        batch.Version.Should().Be(1);

        batch.DomainEvents.Should().ContainSingle(e => e is MfaRecoveryBatchCreatedDomainEvent);
        var evt = (MfaRecoveryBatchCreatedDomainEvent)batch.DomainEvents.Single(e => e is MfaRecoveryBatchCreatedDomainEvent);
        evt.BatchId.Should().Be(batch.Id);
        evt.UserId.Should().Be(userId);
        evt.CodeCount.Should().Be(3);
        evt.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void Create_WithEmptyCodes_ShouldThrow()
    {
        var act = () => MfaRecoveryBatch.Create(Guid.NewGuid(), [], DateTimeOffset.UtcNow, ActorId);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*requires at least one code*");
    }

    [Fact]
    public void Create_WithBlankCodes_ShouldThrow()
    {
        var act = () => MfaRecoveryBatch.Create(Guid.NewGuid(), [" ", null!, ""], DateTimeOffset.UtcNow, ActorId);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*requires at least one code*");
    }

    [Fact]
    public void Create_ShouldDeduplicateIdenticalHashes()
    {
        var batch = MfaRecoveryBatch.Create(Guid.NewGuid(), ["same", "same", "same"], DateTimeOffset.UtcNow, ActorId);

        batch.Codes.Should().HaveCount(1);
    }

    [Fact]
    public void TryConsume_WithMatchingVerifier_ShouldConsumeOnceAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var batch = CreateBatch(createdAt: now);
        ((IHasDomainEvents)batch).ClearDomainEvents();

        var first = batch.TryConsume("hash-b", now.AddMinutes(1), ActorId);
        var second = batch.TryConsume("hash-b", now.AddMinutes(2), ActorId);

        first.Should().BeTrue();
        second.Should().BeFalse();

        batch.Codes.Single(c => c.CodeHash == "hash-b").ConsumedAt.Should().Be(now.AddMinutes(1));
        batch.Codes.Where(c => c.CodeHash != "hash-b").Should().OnlyContain(c => c.ConsumedAt == null);
        batch.Version.Should().Be(2);

        batch.DomainEvents.Should().ContainSingle(e => e is MfaRecoveryCodeConsumedDomainEvent);
        var evt = (MfaRecoveryCodeConsumedDomainEvent)batch.DomainEvents.Single(e => e is MfaRecoveryCodeConsumedDomainEvent);
        evt.BatchId.Should().Be(batch.Id);
        evt.UserId.Should().Be(batch.UserId);
        evt.ConsumedAt.Should().Be(now.AddMinutes(1));
    }

    [Fact]
    public void TryConsume_WithUnknownVerifier_ShouldReturnFalseWithoutMutation()
    {
        var now = DateTimeOffset.UtcNow;
        var batch = CreateBatch(createdAt: now);
        ((IHasDomainEvents)batch).ClearDomainEvents();
        var versionBefore = batch.Version;

        var result = batch.TryConsume("never-issued", now.AddMinutes(1), ActorId);

        result.Should().BeFalse();
        batch.Codes.Should().OnlyContain(c => c.ConsumedAt == null);
        batch.Version.Should().Be(versionBefore);
        batch.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void TryConsume_OnInvalidatedBatch_ShouldReturnFalseWithoutMutation()
    {
        var now = DateTimeOffset.UtcNow;
        var batch = CreateBatch(createdAt: now);
        batch.Invalidate(now.AddMinutes(1), ActorId);
        ((IHasDomainEvents)batch).ClearDomainEvents();
        var versionBefore = batch.Version;

        var result = batch.TryConsume("hash-a", now.AddMinutes(2), ActorId);

        result.Should().BeFalse();
        batch.Codes.Should().OnlyContain(c => c.ConsumedAt == null);
        batch.Version.Should().Be(versionBefore);
        batch.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Invalidate_ShouldInvalidateBatchAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var batch = CreateBatch(createdAt: now);
        ((IHasDomainEvents)batch).ClearDomainEvents();

        batch.Invalidate(now.AddMinutes(1), ActorId);

        batch.InvalidatedAt.Should().Be(now.AddMinutes(1));
        batch.Version.Should().Be(2);

        batch.DomainEvents.Should().ContainSingle(e => e is MfaRecoveryBatchInvalidatedDomainEvent);
        var evt = (MfaRecoveryBatchInvalidatedDomainEvent)batch.DomainEvents.Single(e => e is MfaRecoveryBatchInvalidatedDomainEvent);
        evt.BatchId.Should().Be(batch.Id);
        evt.UserId.Should().Be(batch.UserId);
        evt.InvalidatedAt.Should().Be(now.AddMinutes(1));
    }

    [Fact]
    public void Invalidate_WhenAlreadyInvalidated_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var batch = CreateBatch(createdAt: now);
        batch.Invalidate(now.AddMinutes(1), ActorId);
        ((IHasDomainEvents)batch).ClearDomainEvents();
        var versionBefore = batch.Version;

        batch.Invalidate(now.AddMinutes(2), ActorId);

        batch.InvalidatedAt.Should().Be(now.AddMinutes(1));
        batch.Version.Should().Be(versionBefore);
        batch.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ConsumeAfterInvalidate_ShouldBeRejectedByEarlierBatch()
    {
        var now = DateTimeOffset.UtcNow;

        var oldBatch = CreateBatch(createdAt: now);
        oldBatch.Invalidate(now.AddMinutes(1), ActorId);

        var freshBatch = MfaRecoveryBatch.Create(
            oldBatch.UserId,
            ["new-hash"],
            now.AddMinutes(2),
            ActorId);

        oldBatch.TryConsume("hash-a", now.AddMinutes(3), ActorId).Should().BeFalse();
        freshBatch.TryConsume("new-hash", now.AddMinutes(3), ActorId).Should().BeTrue();
    }
}
