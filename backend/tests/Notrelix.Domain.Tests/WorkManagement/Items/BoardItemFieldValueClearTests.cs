using FluentAssertions;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Domain.Tests.WorkManagement.Items;

/// <summary>
/// WM-V3-APP-ITEM-002 / DOM-ITEM-001: the canonical cleared value (JSON null) is
/// accepted for option-backed fields, while a non-empty value must still name a
/// configured option.
/// </summary>
public class BoardItemFieldValueClearTests
{
    private readonly Guid _actor = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Theory]
    [InlineData(FieldType.Select)]
    [InlineData(FieldType.MultiSelect)]
    public void ClearingAnOptionField_StoresTheCanonicalNull(FieldType type)
    {
        var (item, field, option) = Arrange(type);
        var selected = type == FieldType.MultiSelect ? $"[\"{option.Id}\"]" : $"\"{option.Id}\"";
        item.UpdateFieldValue(field, FieldValue.Create(JsonValue.Create(selected)), _actor, _now);

        item.UpdateFieldValue(field, FieldValue.Empty(), _actor, _now);

        item.FieldValues.Single(v => v.FieldId == field.Id).Value.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void AnUnknownOption_IsStillRejected()
    {
        var (item, field, _) = Arrange(FieldType.Select);

        var act = () => item.UpdateFieldValue(field, FieldValue.Create(JsonValue.Create($"\"{Guid.NewGuid()}\"")), _actor, _now);

        act.Should().Throw<BusinessRuleException>();
        item.FieldValues.Should().BeEmpty();
    }

    private (BoardItem Item, BoardField Field, FieldOption Option) Arrange(FieldType type)
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var field = BoardField.Create(accountId, workspaceId, boardId, "Priority", type, FieldSettings.Empty(), FractionalIndex.Create("a0"), _actor, _now);
        var option = field.Options.FirstOrDefault() ?? field.AddOption("High", Color.Create("#FF0000"), FractionalIndex.Create("a5"), _actor, _now);
        var item = BoardItem.CreateRoot(accountId, workspaceId, boardId, Guid.NewGuid(), "Task", FractionalIndex.Create("a0"), _actor, _now);
        return (item, field, option);
    }
}
