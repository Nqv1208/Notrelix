using FluentAssertions;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Domain.Tests.WorkManagement.Fields;

/// <summary>WM-V3-APP-OPTION-001: one option moves by rewriting only its own key.</summary>
public class FieldOptionMoveTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    private BoardField CreateSelectWithTwoOptions(out FieldOption a, out FieldOption b)
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Priority", FieldType.Select,
            FieldSettings.Empty(), FractionalIndex.Create("a0"), _actorId, _now);
        a = field.AddOption("A", Color.Create("#FF0000"), FractionalIndex.Create("a1"), _actorId, _now);
        b = field.AddOption("B", Color.Create("#00FF00"), FractionalIndex.Create("a3"), _actorId, _now);
        ((IHasDomainEvents)field).ClearDomainEvents();
        return field;
    }

    [Fact]
    public void MoveOption_RewritesOnlyTheMovedKey_AndRaisesOneMovedEvent()
    {
        var field = CreateSelectWithTwoOptions(out var a, out var b);
        var version = field.Version;

        field.MoveOption(a.Id, FractionalIndex.Create("a5"), _actorId, _now);

        a.Position.Value.Should().Be("a5");
        b.Position.Value.Should().Be("a3", "the neighbor key is never rewritten by a single move");
        field.Version.Should().Be(version + 1);
        field.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<FieldOptionMovedDomainEvent>()
            .Which.Should().Match<FieldOptionMovedDomainEvent>(e => e.OptionId == a.Id && e.NewPosition.Value == "a5" && e.FieldId == field.Id);
    }

    [Fact]
    public void MoveOption_ToItsCurrentKey_IsASemanticNoOp()
    {
        var field = CreateSelectWithTwoOptions(out var a, out _);
        var version = field.Version;

        field.MoveOption(a.Id, FractionalIndex.Create("a1"), _actorId, _now);

        field.Version.Should().Be(version);
        field.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MoveOption_OfUnknownOption_IsRejected_AndChangesNothing()
    {
        var field = CreateSelectWithTwoOptions(out var a, out _);
        var version = field.Version;

        var act = () => field.MoveOption(Guid.NewGuid(), FractionalIndex.Create("a5"), _actorId, _now);

        act.Should().Throw<BusinessRuleException>();
        a.Position.Value.Should().Be("a1");
        field.Version.Should().Be(version);
        field.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MoveOption_OnDeletedField_IsRejected()
    {
        var field = CreateSelectWithTwoOptions(out var a, out _);
        field.Delete(_actorId, _now);

        var act = () => field.MoveOption(a.Id, FractionalIndex.Create("a5"), _actorId, _now);

        act.Should().Throw<Exception>();
        a.Position.Value.Should().Be("a1");
    }

    [Fact]
    public void AddOption_ReturnsTheCreatedOption()
    {
        var field = CreateSelectWithTwoOptions(out _, out _);

        var created = field.AddOption("C", Color.Create("#0000FF"), FractionalIndex.Create("a7"), _actorId, _now);

        field.Options.Should().Contain(created);
        created.Name.Should().Be("C");
    }
}
