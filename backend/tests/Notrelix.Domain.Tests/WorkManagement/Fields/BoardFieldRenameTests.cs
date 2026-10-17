using FluentAssertions;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Domain.Tests.WorkManagement.Fields;

/// <summary>
/// FIELD-DOM-001 / FIELD-APP-001 (WM-V3-APP-FIELD-001, WMREQ026/030). Rename keeps
/// the field identity, trims and bounds the name, increments version once with one
/// renamed event, treats the current name as a no-op, and rejects invalid input or
/// a deleted field without mutating anything.
/// </summary>
public class BoardFieldRenameTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);

    private static BoardField NewField(string name = "Notes")
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), name, FieldType.Text,
            FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), Now);
        ((IHasDomainEvents)field).ClearDomainEvents();
        return field;
    }

    [Fact]
    public void Rename_ChangesTheTrimmedName_OnceWithOneEvent()
    {
        var field = NewField();
        var id = field.Id;
        var version = field.Version;

        field.Rename("  Remarks  ", Guid.NewGuid(), Now.AddMinutes(1));

        field.Id.Should().Be(id);
        field.Name.Should().Be("Remarks");
        field.Version.Should().Be(version + 1);
        field.DomainEvents.OfType<BoardFieldRenamedDomainEvent>().Should().ContainSingle()
            .Which.Should().Match<BoardFieldRenamedDomainEvent>(e => e.OldName == "Notes" && e.NewName == "Remarks");
    }

    [Fact]
    public void Rename_ToTheCurrentName_IsANoOp()
    {
        var field = NewField();
        var version = field.Version;

        field.Rename(" Notes ", Guid.NewGuid(), Now.AddMinutes(1));

        field.Version.Should().Be(version);
        field.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_ToAnEmptyName_IsRejectedWithoutMutation(string name)
    {
        var field = NewField();
        var version = field.Version;

        var act = () => field.Rename(name, Guid.NewGuid(), Now);

        act.Should().Throw<Exception>();
        field.Name.Should().Be("Notes");
        field.Version.Should().Be(version);
    }

    [Fact]
    public void Rename_AboveTheDomainLimit_IsRejectedWithoutMutation()
    {
        var field = NewField();

        var act = () => field.Rename(new string('n', 101), Guid.NewGuid(), Now);

        act.Should().Throw<Exception>();
        field.Name.Should().Be("Notes");
    }

    [Fact]
    public void Rename_OfADeletedField_IsRejected()
    {
        var field = NewField();
        field.Delete(Guid.NewGuid(), Now);

        var act = () => field.Rename("Remarks", Guid.NewGuid(), Now.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>();
    }
}
