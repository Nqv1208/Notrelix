using FluentAssertions;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.WorkManagement.Fields;

public class FieldOptionsReorderTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _boardId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(BoardField), nameof(BoardField.ReorderOptions), MutationScenario.Event, typeof(System.Collections.Generic.IReadOnlyList<System.Guid>), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void ReorderOptions_ShouldRaiseEvent_WithOrderedOptionIds()
    {
        var field = BoardField.Create(_accountId, _workspaceId, _boardId, "Status", FieldType.Select,
            FieldSettings.Empty(), FractionalIndex.Create("a0"), _actorId, _now);
        field.AddOption("A", Color.Create("#FF0000"), FractionalIndex.Create("a1"), _actorId, _now);
        field.AddOption("B", Color.Create("#00FF00"), FractionalIndex.Create("a3"), _actorId, _now);
        var optionA = field.Options.First(o => o.Name == "A");
        var optionB = field.Options.First(o => o.Name == "B");
        ((IHasDomainEvents)field).ClearDomainEvents();

        field.ReorderOptions(new List<Guid> { optionB.Id, optionA.Id }, _actorId, _now);

        field.DomainEvents.Should().ContainSingle(e => e is FieldOptionsReorderedDomainEvent);
        var evt = (FieldOptionsReorderedDomainEvent)field.DomainEvents.Single();
        evt.AccountId.Should().Be(_accountId);
        evt.WorkspaceId.Should().Be(_workspaceId);
        evt.BoardId.Should().Be(_boardId);
        evt.FieldId.Should().Be(field.Id);
        evt.OrderedOptionIds.Should().BeEquivalentTo(new[] { optionB.Id, optionA.Id });
        evt.OrderedOptionIds.Should().HaveCount(2);
        evt.OrderedOptionIds[0].Should().Be(optionB.Id);
        evt.OrderedOptionIds[1].Should().Be(optionA.Id);
    }

    [CoversMutation(typeof(BoardField), nameof(BoardField.ReorderOptions), MutationScenario.Event, typeof(System.Collections.Generic.IReadOnlyList<System.Guid>), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void ReorderOptions_ShouldNotRaiseEvent_WhenSameOrder()
    {
        var field = BoardField.Create(_accountId, _workspaceId, _boardId, "Status", FieldType.Select,
            FieldSettings.Empty(), FractionalIndex.Create("a0"), _actorId, _now);
        field.AddOption("A", Color.Create("#FF0000"), FractionalIndex.Create("a1"), _actorId, _now);
        field.AddOption("B", Color.Create("#00FF00"), FractionalIndex.Create("a3"), _actorId, _now);
        var optionA = field.Options.First(o => o.Name == "A");
        var optionB = field.Options.First(o => o.Name == "B");
        ((IHasDomainEvents)field).ClearDomainEvents();
        var version = field.Version;

        field.ReorderOptions(new List<Guid> { optionA.Id, optionB.Id }, _actorId, _now);

        field.DomainEvents.Should().BeEmpty();
        field.Version.Should().Be(version);
    }

    [CoversMutation(typeof(BoardField), nameof(BoardField.ReorderOptions), MutationScenario.Invalid, typeof(System.Collections.Generic.IReadOnlyList<System.Guid>), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void ReorderOptions_ShouldThrow_WhenDuplicateIds()
    {
        var field = BoardField.Create(_accountId, _workspaceId, _boardId, "Status", FieldType.Select,
            FieldSettings.Empty(), FractionalIndex.Create("a0"), _actorId, _now);
        field.AddOption("A", Color.Create("#FF0000"), FractionalIndex.Create("a1"), _actorId, _now);
        field.AddOption("B", Color.Create("#00FF00"), FractionalIndex.Create("a3"), _actorId, _now);
        var optionA = field.Options.First(o => o.Name == "A");

        Action act = () => field.ReorderOptions(new List<Guid> { optionA.Id, optionA.Id }, _actorId, _now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*duplicate*");
    }

    [CoversMutation(typeof(BoardField), nameof(BoardField.ReorderOptions), MutationScenario.Invalid, typeof(System.Collections.Generic.IReadOnlyList<System.Guid>), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void ReorderOptions_ShouldThrow_WhenExtraIds()
    {
        var field = BoardField.Create(_accountId, _workspaceId, _boardId, "Status", FieldType.Select,
            FieldSettings.Empty(), FractionalIndex.Create("a0"), _actorId, _now);
        field.AddOption("A", Color.Create("#FF0000"), FractionalIndex.Create("a1"), _actorId, _now);
        var optionA = field.Options.First(o => o.Name == "A");

        Action act = () => field.ReorderOptions(new List<Guid> { optionA.Id, Guid.NewGuid() }, _actorId, _now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*all options*");
    }

    [CoversMutation(typeof(BoardField), nameof(BoardField.ReorderOptions), MutationScenario.Valid, typeof(System.Collections.Generic.IReadOnlyList<System.Guid>), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void ReorderOptions_ShouldApplyNewPositions()
    {
        var field = BoardField.Create(_accountId, _workspaceId, _boardId, "Status", FieldType.Select,
            FieldSettings.Empty(), FractionalIndex.Create("a0"), _actorId, _now);
        field.AddOption("A", Color.Create("#FF0000"), FractionalIndex.Create("a1"), _actorId, _now);
        field.AddOption("B", Color.Create("#00FF00"), FractionalIndex.Create("a3"), _actorId, _now);
        var optionA = field.Options.First(o => o.Name == "A");
        var optionB = field.Options.First(o => o.Name == "B");

        field.ReorderOptions(new List<Guid> { optionB.Id, optionA.Id }, _actorId, _now);

        var reorderedA = field.Options.First(o => o.Id == optionA.Id);
        var reorderedB = field.Options.First(o => o.Id == optionB.Id);
        reorderedB.Position.Value.Should().Be("a0");
        reorderedA.Position.Value.Should().Be("a1");
    }
}
