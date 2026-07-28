using FluentAssertions;

namespace Notrelix.Domain.Tests.Common;

public class TwoPhaseAuditTests
{
    private class TestAuditableEntity : AuditableEntity
    {
        public string BusinessState { get; private set; } = "initial";

        public void SetBusinessState(string value) => BusinessState = value;

        public void PublicSetAuditOnCreate(Guid? actor, DateTimeOffset time)
            => SetAuditOnCreate(actor, time);

        public void PublicSetAuditOnUpdate(Guid? actor, DateTimeOffset time)
            => SetAuditOnUpdate(actor, time);

        public PendingAuditUpdate PublicPrepareAuditUpdate(Guid? actor, DateTimeOffset time)
            => PrepareAuditUpdate(actor, time);

        public void PublicApplyAuditUpdate(PendingAuditUpdate update)
            => ApplyAuditUpdate(update);
    }

    [Fact]
    public void PrepareAuditUpdate_ShouldValidateWithoutMutating()
    {
        var entity = new TestAuditableEntity();
        entity.PublicSetAuditOnCreate(Guid.NewGuid(), new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var update = entity.PublicPrepareAuditUpdate(Guid.NewGuid(), new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));

        entity.UpdatedAt.Should().BeNull("PrepareAuditUpdate must not mutate state");
        entity.UpdatedBy.Should().BeNull("PrepareAuditUpdate must not mutate state");
        update.ActorId.Should().NotBeNull();
        update.OccurredAt.Should().Be(new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void ApplyAuditUpdate_ShouldChangeOnlyUpdatedAtAndUpdatedBy()
    {
        var entity = new TestAuditableEntity();
        entity.PublicSetAuditOnCreate(Guid.NewGuid(), new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        entity.SetBusinessState("modified");

        var actor = Guid.NewGuid();
        var time = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var update = entity.PublicPrepareAuditUpdate(actor, time);

        entity.PublicApplyAuditUpdate(update);

        entity.UpdatedAt.Should().Be(time);
        entity.UpdatedBy.Should().Be(actor);
        entity.BusinessState.Should().Be("modified", "ApplyAuditUpdate must not touch business state");
    }

    [Fact]
    public void InvalidAuditActor_ShouldThrowAndLeaveAggregateUnchanged()
    {
        var entity = new TestAuditableEntity();
        entity.PublicSetAuditOnCreate(Guid.NewGuid(), new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        entity.SetBusinessState("before");

        var act = () => entity.PublicPrepareAuditUpdate(Guid.Empty, new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));

        act.Should().Throw<BusinessRuleException>();
        entity.UpdatedAt.Should().BeNull();
        entity.UpdatedBy.Should().BeNull();
        entity.BusinessState.Should().Be("before");
    }

    [Fact]
    public void InvalidAuditTimestamp_ShouldThrowAndLeaveAggregateUnchanged()
    {
        var entity = new TestAuditableEntity();
        entity.PublicSetAuditOnCreate(Guid.NewGuid(), new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        entity.SetBusinessState("before");

        var act = () => entity.PublicPrepareAuditUpdate(Guid.NewGuid(), default);

        act.Should().Throw<BusinessRuleException>();
        entity.UpdatedAt.Should().BeNull();
        entity.UpdatedBy.Should().BeNull();
        entity.BusinessState.Should().Be("before");
    }

    [Fact]
    public void TimestampRegression_ShouldThrowAndLeaveAggregateUnchanged()
    {
        var entity = new TestAuditableEntity();
        entity.PublicSetAuditOnCreate(Guid.NewGuid(), new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));
        entity.PublicSetAuditOnUpdate(Guid.NewGuid(), new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero));
        entity.SetBusinessState("before");

        var act = () => entity.PublicPrepareAuditUpdate(Guid.NewGuid(), new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero));

        act.Should().Throw<BusinessRuleException>();
        entity.UpdatedAt.Should().Be(new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero));
        entity.UpdatedBy.Should().NotBeNull();
        entity.BusinessState.Should().Be("before");
    }

    [Fact]
    public void TimestampBeforeCreatedAt_ShouldThrowAndLeaveAggregateUnchanged()
    {
        var entity = new TestAuditableEntity();
        entity.PublicSetAuditOnCreate(Guid.NewGuid(), new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));
        entity.SetBusinessState("before");

        var act = () => entity.PublicPrepareAuditUpdate(Guid.NewGuid(), new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        act.Should().Throw<BusinessRuleException>();
        entity.UpdatedAt.Should().BeNull();
        entity.BusinessState.Should().Be("before");
    }

    [Fact]
    public void SetAuditOnUpdate_ShouldStillWork_ForBackwardCompatibility()
    {
        var entity = new TestAuditableEntity();
        entity.PublicSetAuditOnCreate(Guid.NewGuid(), new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        entity.PublicSetAuditOnUpdate(Guid.NewGuid(), new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));

        entity.UpdatedAt.Should().Be(new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));
        entity.UpdatedBy.Should().NotBeNull();
    }

    [Fact]
    public void NullableActor_ForSystemOperations_ShouldBeAccepted()
    {
        var entity = new TestAuditableEntity();

        var act = () => entity.PublicSetAuditOnCreate(null, new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        act.Should().NotThrow();
        entity.CreatedBy.Should().BeNull();
        entity.CreatedAt.Should().Be(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }
}
