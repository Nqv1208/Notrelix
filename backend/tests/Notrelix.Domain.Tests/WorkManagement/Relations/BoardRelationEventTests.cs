using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.WorkManagement.Relations;

namespace Notrelix.Domain.Tests.WorkManagement.Relations;

[CoversAggregate(typeof(BoardRelation))]
public class BoardRelationEventTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(BoardRelation), "Pause(System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(BoardRelation), "Delete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [CoversMutation(typeof(BoardRelation), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void BoardRelation_Pause_ShouldRaiseEvent()
    {
        var relation = BoardRelation.Create(Guid.NewGuid(), WsA, BoardA, Guid.NewGuid(), null, null, Actor, Now);
        ((IHasDomainEvents)relation).ClearDomainEvents();
        var version = relation.Version;

        relation.Pause(Actor, Now);

        relation.Status.Should().Be(BoardRelationStatus.Paused);
        relation.Version.Should().Be(version + 1);
        relation.DomainEvents.Should().ContainSingle(e => e is BoardRelationPausedDomainEvent);
    }

    [CoversMutation(typeof(BoardRelation), "Pause(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void BoardRelation_Pause_WhenAlreadyPaused_ShouldNotRaiseEvent()
    {
        var relation = BoardRelation.Create(Guid.NewGuid(), WsA, BoardA, Guid.NewGuid(), null, null, Actor, Now);
        relation.Pause(Actor, Now);
        ((IHasDomainEvents)relation).ClearDomainEvents();
        var version = relation.Version;

        relation.Pause(Actor, Now);

        relation.Version.Should().Be(version);
        relation.DomainEvents.Should().NotContain(e => e is BoardRelationPausedDomainEvent);
    }

    [CoversMutation(typeof(BoardRelation), "Resume(System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void BoardRelation_Resume_ShouldRaiseEvent()
    {
        var relation = BoardRelation.Create(Guid.NewGuid(), WsA, BoardA, Guid.NewGuid(), null, null, Actor, Now);
        relation.Pause(Actor, Now);
        ((IHasDomainEvents)relation).ClearDomainEvents();
        var version = relation.Version;

        relation.Resume(Actor, Now);

        relation.Status.Should().Be(BoardRelationStatus.Active);
        relation.Version.Should().Be(version + 1);
        relation.DomainEvents.Should().ContainSingle(e => e is BoardRelationResumedDomainEvent);
    }

    [CoversMutation(typeof(BoardRelation), "Resume(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void BoardRelation_Resume_WhenAlreadyActive_ShouldNotRaiseEvent()
    {
        var relation = BoardRelation.Create(Guid.NewGuid(), WsA, BoardA, Guid.NewGuid(), null, null, Actor, Now);
        ((IHasDomainEvents)relation).ClearDomainEvents();
        var version = relation.Version;

        relation.Resume(Actor, Now);

        relation.Version.Should().Be(version);
        relation.DomainEvents.Should().NotContain(e => e is BoardRelationResumedDomainEvent);
    }

    [CoversMutation(typeof(BoardRelation), "MarkBroken(System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void BoardRelation_MarkBroken_ShouldRaiseEvent()
    {
        var relation = BoardRelation.Create(Guid.NewGuid(), WsA, BoardA, Guid.NewGuid(), null, null, Actor, Now);
        ((IHasDomainEvents)relation).ClearDomainEvents();
        var version = relation.Version;

        relation.MarkBroken(Actor, Now);

        relation.Status.Should().Be(BoardRelationStatus.Broken);
        relation.Version.Should().Be(version + 1);
        relation.DomainEvents.Should().ContainSingle(e => e is BoardRelationMarkedBrokenDomainEvent);
    }

    [CoversMutation(typeof(BoardRelation), "MarkBroken(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void BoardRelation_MarkBroken_WhenAlreadyBroken_ShouldNotRaiseEvent()
    {
        var relation = BoardRelation.Create(Guid.NewGuid(), WsA, BoardA, Guid.NewGuid(), null, null, Actor, Now);
        relation.MarkBroken(Actor, Now);
        ((IHasDomainEvents)relation).ClearDomainEvents();
        var version = relation.Version;

        relation.MarkBroken(Actor, Now);

        relation.Version.Should().Be(version);
        relation.DomainEvents.Should().NotContain(e => e is BoardRelationMarkedBrokenDomainEvent);
    }
}
