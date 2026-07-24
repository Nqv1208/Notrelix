using FluentAssertions;

namespace Notrelix.Domain.Tests.Common;

public class SoftDeletableEntityTests
{
    private class TestEntity : SoftDeletableEntity
    {
        public bool PublicMarkDeleted(Guid? deletedBy, DateTimeOffset deletedAt, string? reason = null)
            => MarkDeleted(deletedBy, deletedAt, reason);

        public bool PublicMarkRestored(Guid? restoredBy, DateTimeOffset restoredAt)
            => MarkRestored(restoredBy, restoredAt);

        public void PublicEnsureNotDeleted() => EnsureNotDeleted();
    }

    [Fact]
    public void MarkDeleted_ShouldSetDeletedProperties()
    {
        var entity = new TestEntity();
        var deletedBy = Guid.NewGuid();
        var deletedAt = DateTimeOffset.UtcNow;

        var result = entity.PublicMarkDeleted(deletedBy, deletedAt, "Cleanup");

        result.Should().BeTrue();
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(deletedAt);
        entity.DeletedBy.Should().Be(deletedBy);
        entity.DeleteReason.Should().Be("Cleanup");
    }

    [Fact]
    public void MarkDeleted_AlreadyDeleted_ShouldReturnFalse()
    {
        var entity = new TestEntity();
        var time = DateTimeOffset.UtcNow;

        entity.PublicMarkDeleted(Guid.NewGuid(), time);
        var result = entity.PublicMarkDeleted(Guid.NewGuid(), time.AddMinutes(1));

        result.Should().BeFalse();
    }

    [Fact]
    public void MarkDeleted_InvalidTimestamp_ShouldThrow()
    {
        var entity = new TestEntity();
        var act = () => entity.PublicMarkDeleted(Guid.NewGuid(), default);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void MarkRestored_ShouldClearDeletedProperties()
    {
        var entity = new TestEntity();
        var userId = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;

        entity.PublicMarkDeleted(userId, time);
        var result = entity.PublicMarkRestored(userId, time.AddMinutes(1));

        result.Should().BeTrue();
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
        entity.DeleteReason.Should().BeNull();
        entity.RestoredAt.Should().Be(time.AddMinutes(1));
        entity.RestoredBy.Should().Be(userId);
    }

    [Fact]
    public void MarkRestored_NotDeleted_ShouldReturnFalse()
    {
        var entity = new TestEntity();
        var result = entity.PublicMarkRestored(Guid.NewGuid(), DateTimeOffset.UtcNow);

        result.Should().BeFalse();
    }

    [Fact]
    public void MarkRestored_InvalidTimestamp_ShouldThrow()
    {
        var entity = new TestEntity();
        entity.PublicMarkDeleted(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => entity.PublicMarkRestored(Guid.NewGuid(), default);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void EnsureNotDeleted_ShouldThrow_WhenDeleted()
    {
        var entity = new TestEntity();
        entity.PublicMarkDeleted(Guid.NewGuid(), DateTimeOffset.UtcNow);

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
