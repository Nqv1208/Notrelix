using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Domain.Tests.WorkManagement.Items;

public class BoardItemOptionIdentityTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static BoardItem CreateItem()
    {
        return BoardItem.CreateRoot(AccountId, WsA, BoardA, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Actor, Now);
    }

    private static BoardField CreateSelectField()
    {
        var field = BoardField.Create(AccountId, WsA, BoardA, "Status", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a0"), Actor, Now);
        field.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("a1"), Actor, Now);
        return field;
    }

    private static BoardField CreateMultiSelectField()
    {
        var field = BoardField.Create(AccountId, WsA, BoardA, "Tags", FieldType.MultiSelect, FieldSettings.Empty(), FractionalIndex.Create("a0"), Actor, Now);
        field.AddOption("Alpha", Color.Create("#00FF00"), FractionalIndex.Create("a1"), Actor, Now);
        field.AddOption("Beta", Color.Create("#00FF00"), FractionalIndex.Create("a2"), Actor, Now);
        return field;
    }

    [CoversMutation(typeof(BoardItem), nameof(BoardItem.UpdateFieldValue), MutationScenario.Valid, typeof(BoardField), typeof(FieldValue), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void SelectValue_WithUppercaseBracedGuid_ShouldBeAccepted()
    {
        var item = CreateItem();
        var field = CreateSelectField();
        var optionId = field.Options.First().Id;
        var raw = "{" + optionId.ToString().ToUpperInvariant() + "}";
        var value = FieldValue.Create(JsonValue.Create($"\"{raw}\""));

        item.UpdateFieldValue(field, value, Actor, Now);

        item.FieldValues.Should().ContainSingle();
        item.FieldValues.First().Value.Should().Be(value);
    }

    [CoversMutation(typeof(BoardItem), nameof(BoardItem.UpdateFieldValue), MutationScenario.Invalid, typeof(BoardField), typeof(FieldValue), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void SelectValue_WithNonParsableId_ShouldThrow()
    {
        var item = CreateItem();
        var field = CreateSelectField();

        var act = () => item.UpdateFieldValue(field, FieldValue.Create(JsonValue.Create("\"not-a-guid\"")), Actor, Now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*GUID*");
    }

    [CoversMutation(typeof(BoardItem), nameof(BoardItem.UpdateFieldValue), MutationScenario.Valid, typeof(BoardField), typeof(FieldValue), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void MultiSelectValue_WithUppercaseBracedGuids_ShouldBeAccepted()
    {
        var item = CreateItem();
        var field = CreateMultiSelectField();
        var ids = field.Options.Select(o => "{" + o.Id.ToString().ToUpperInvariant() + "}").ToList();
        var value = FieldValue.Create(JsonValue.Create($"[ \"{ids[0]}\", \"{ids[1]}\" ]"));

        item.UpdateFieldValue(field, value, Actor, Now);

        item.FieldValues.Should().ContainSingle();
    }

    [CoversMutation(typeof(BoardItem), nameof(BoardItem.UpdateFieldValue), MutationScenario.Invalid, typeof(BoardField), typeof(FieldValue), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void MultiSelectValue_WithDuplicateOptionIds_ShouldThrow()
    {
        var item = CreateItem();
        var field = CreateMultiSelectField();
        var id = field.Options.First().Id;
        var value = FieldValue.Create(JsonValue.Create($"[ \"{id}\", \"{id}\" ]"));

        var act = () => item.UpdateFieldValue(field, value, Actor, Now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*duplicate*");
        item.FieldValues.Should().BeEmpty();
    }

    [CoversMutation(typeof(BoardItem), nameof(BoardItem.UpdateFieldValue), MutationScenario.Invalid, typeof(BoardField), typeof(FieldValue), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void MultiSelectValue_WithUnknownOption_ShouldThrow()
    {
        var item = CreateItem();
        var field = CreateMultiSelectField();
        var id = field.Options.First().Id;
        var value = FieldValue.Create(JsonValue.Create($"[ \"{id}\", \"{Guid.NewGuid()}\" ]"));

        var act = () => item.UpdateFieldValue(field, value, Actor, Now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*option*");
        item.FieldValues.Should().BeEmpty();
    }
}
