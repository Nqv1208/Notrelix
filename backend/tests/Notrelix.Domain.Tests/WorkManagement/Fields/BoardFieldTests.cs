using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Domain.Tests.WorkManagement;

[CoversAggregate(typeof(BoardField))]
public class BoardFieldTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var settings = FieldSettings.Create(JsonValue.Create("{\"required\":true}"));
        var position = FractionalIndex.Create("a0");

        var field = BoardField.Create(accountId, workspaceId, boardId, "Due Date", FieldType.Date, settings, position, createdBy, DateTimeOffset.UtcNow);

        field.Name.Should().Be("Due Date");
        field.Type.Should().Be(FieldType.Date);
        field.Settings.Should().Be(settings);
        field.DomainEvents.Should().ContainSingle(e => e is BoardFieldCreatedDomainEvent);
    }

    [Fact]
    public void AddOption_ShouldSucceed_AndRaiseEvent()
    {
        var settings = FieldSettings.Create(JsonValue.EmptyObject());
        var position = FractionalIndex.Create("a0");
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Select, settings, position, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)field).ClearDomainEvents();

        field.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        field.Options.Should().HaveCount(1);
        field.Options.First().Name.Should().Be("Done");
        field.DomainEvents.Should().ContainSingle(e => e is FieldOptionAddedDomainEvent);
    }

    [Fact]
    public void AddOption_ShouldThrow_WhenDuplicateName()
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => field.AddOption("Done", Color.Create("#FF0000"), FractionalIndex.Create("a3"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*duplicate*");
    }

    [Fact]
    public void AddOption_ShouldAllow_WhenSameNameDifferentCase()
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => field.AddOption("done", Color.Create("#FF0000"), FractionalIndex.Create("a3"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*duplicate*");
    }

    [Fact]
    public void AddOption_ShouldThrow_WhenFieldTypeIsText()
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Title", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => field.AddOption("Option", Color.Create("#00FF00"), FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*cannot add options*");
    }

    [Fact]
    public void RemoveOption_ShouldSucceed_AndRaiseEvent()
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var optionId = field.Options.First().Id;
        ((IHasDomainEvents)field).ClearDomainEvents();

        field.RemoveOption(optionId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        field.Options.Should().BeEmpty();
        field.DomainEvents.Should().ContainSingle(e => e is FieldOptionRemovedDomainEvent);
    }

    [Fact]
    public void RemoveOption_ShouldThrow_WhenNotFound()
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => field.RemoveOption(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void UpdateOption_ShouldSucceed_AndRaiseEvent()
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var optionId = field.Options.First().Id;
        ((IHasDomainEvents)field).ClearDomainEvents();

        field.UpdateOption(optionId, "Completed", Color.Create("#FF0000"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        field.Options.First().Name.Should().Be("Completed");
        field.DomainEvents.Should().ContainSingle(e => e is FieldOptionUpdatedDomainEvent);
    }

    [Fact]
    public void UpdateOption_ShouldThrow_WhenDuplicateName()
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("In Progress", Color.Create("#FFFF00"), FractionalIndex.Create("a3"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var optionId = field.Options.First().Id;

        Action act = () => field.UpdateOption(optionId, "In Progress", Color.Create("#FF0000"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*duplicate*");
    }

    [Fact]
    public void ReorderOptions_ShouldSucceed_AndRaiseEvent()
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("A", Color.Create("#FF0000"), FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("B", Color.Create("#00FF00"), FractionalIndex.Create("a3"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var optionA = field.Options.First(o => o.Name == "A");
        var optionB = field.Options.First(o => o.Name == "B");
        ((IHasDomainEvents)field).ClearDomainEvents();

        field.ReorderOptions(new List<Guid> { optionB.Id, optionA.Id }, Guid.NewGuid(), DateTimeOffset.UtcNow);

        field.Options.First(o => o.Id == optionB.Id).Position.Value.Should().Be("a0");
        field.Options.First(o => o.Id == optionA.Id).Position.Value.Should().Be("a1");
        field.DomainEvents.Should().ContainSingle(e => e is FieldOptionsReorderedDomainEvent);
        var reorderEvt = (FieldOptionsReorderedDomainEvent)field.DomainEvents.Single(e => e is FieldOptionsReorderedDomainEvent);
        reorderEvt.OrderedOptionIds.Should().BeEquivalentTo(new[] { optionB.Id, optionA.Id });
    }

    [Fact]
    public void ReorderOptions_ShouldThrow_WhenCountMismatch()
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("A", Color.Create("#FF0000"), FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => field.ReorderOptions(new List<Guid>(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*all options*");
    }

    [Fact]
    public void ReorderOptions_ShouldThrow_WhenMissingOption()
    {
        var field = BoardField.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Status", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("A", Color.Create("#FF0000"), FractionalIndex.Create("a1"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.AddOption("B", Color.Create("#00FF00"), FractionalIndex.Create("a3"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var optionA = field.Options.First(o => o.Name == "A");

        Action act = () => field.ReorderOptions(new List<Guid> { optionA.Id, Guid.NewGuid() }, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }
}
