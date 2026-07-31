using FluentAssertions;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.WorkManagement.Fields;

public class BoardFieldEventTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(BoardField), nameof(BoardField.UpdateClassification), MutationScenario.Event, typeof(DataClassification), typeof(bool), typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(BoardField), nameof(BoardField.UpdateSettings), MutationScenario.Event, typeof(FieldSettings), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void BoardField_UpdateClassification_ShouldRaiseEvent()
    {
        var position = FractionalIndex.Create("a0");
        var field = BoardField.Create(Guid.NewGuid(), WsA, BoardA, "Field", FieldType.Text, FieldSettings.Empty(), position, Actor, Now);
        ((IHasDomainEvents)field).ClearDomainEvents();
        var version = field.Version;

        field.UpdateClassification(DataClassification.Confidential, true, Actor, Now);

        field.Version.Should().Be(version + 1);
        field.DomainEvents.Should().ContainSingle(e => e is BoardFieldClassificationUpdatedDomainEvent);
        var evt = (BoardFieldClassificationUpdatedDomainEvent)field.DomainEvents.Single(e => e is BoardFieldClassificationUpdatedDomainEvent);
        evt.Classification.Should().Be(DataClassification.Confidential);
        evt.IsSensitive.Should().BeTrue();
    }

    [CoversMutation(typeof(BoardField), nameof(BoardField.UpdateClassification), MutationScenario.Event, typeof(DataClassification), typeof(bool), typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(BoardField), nameof(BoardField.UpdateSettings), MutationScenario.Event, typeof(FieldSettings), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void BoardField_UpdateClassification_WhenSameValue_ShouldNotRaiseEvent()
    {
        var position = FractionalIndex.Create("a0");
        var field = BoardField.Create(Guid.NewGuid(), WsA, BoardA, "Field", FieldType.Text, FieldSettings.Empty(), position, Actor, Now,
            dataClassification: DataClassification.Confidential, isSensitive: true);
        ((IHasDomainEvents)field).ClearDomainEvents();
        var version = field.Version;

        field.UpdateClassification(DataClassification.Confidential, true, Actor, Now);

        field.Version.Should().Be(version);
        field.DomainEvents.Should().NotContain(e => e is BoardFieldClassificationUpdatedDomainEvent);
    }

    [CoversMutation(typeof(BoardField), nameof(BoardField.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void BoardField_Restore_ShouldRaiseEvent()
    {
        var position = FractionalIndex.Create("a0");
        var field = BoardField.Create(Guid.NewGuid(), WsA, BoardA, "Field", FieldType.Text, FieldSettings.Empty(), position, Actor, Now);
        field.Delete(Actor, Now);
        ((IHasDomainEvents)field).ClearDomainEvents();
        var version = field.Version;

        field.Restore(Actor, Now);

        field.IsDeleted.Should().BeFalse();
        field.Version.Should().Be(version + 1);
        field.DomainEvents.Should().ContainSingle(e => e is BoardFieldRestoredDomainEvent);
    }

    [CoversMutation(typeof(BoardField), nameof(BoardField.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(BoardField), nameof(BoardField.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void BoardField_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var position = FractionalIndex.Create("a0");
        var field = BoardField.Create(Guid.NewGuid(), WsA, BoardA, "Field", FieldType.Text, FieldSettings.Empty(), position, Actor, Now);
        ((IHasDomainEvents)field).ClearDomainEvents();
        var version = field.Version;

        field.Restore(Actor, Now);

        field.Version.Should().Be(version);
        field.DomainEvents.Should().NotContain(e => e is BoardFieldRestoredDomainEvent);
    }
}
