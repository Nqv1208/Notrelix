using FluentAssertions;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Domain.Tests.WorkManagement.Items;

/// <summary>
/// A system field's definition is protected, not the Item values it holds: an
/// Item's value of a system field (the board's Status, Due Date, Assignee) is
/// written like any other value, while deleting the system field is still
/// rejected.
/// </summary>
public class SystemFieldValueTests
{
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid Board = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 3, 40, 0, TimeSpan.Zero);

    private static BoardField SystemStatus()
    {
        var field = BoardField.Create(Account, Workspace, Board, "Status", FieldType.Status,
            FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")), FractionalIndex.Create("a1"), Actor, Now, isSystem: true);
        field.AddDefaultStatusOptions(Actor, Now);
        return field;
    }

    private static BoardItem Item() =>
        BoardItem.CreateRoot(Account, Workspace, Board, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Actor, Now, "1", 1);

    [Fact]
    public void WritingAValueOfASystemStatusField_Succeeds_AndBumpsTheItemVersion()
    {
        var status = SystemStatus();
        var item = Item();
        var version = item.Version;
        var done = status.Options.Single(o => o.Category == WorkStatusCategory.Done).Id;

        item.UpdateFieldValue(status, FieldValue.Create(JsonValue.Create($"\"{done}\"")), Actor, Now.AddMinutes(1));

        item.FieldValues.Should().ContainSingle(v => v.FieldId == status.Id).Which.Value.Data.Value.Should().Be($"\"{done}\"");
        item.Version.Should().Be(version + 1);
    }

    [Fact]
    public void WritingAValueOfASystemDateField_Succeeds()
    {
        var due = BoardField.Create(Account, Workspace, Board, "Due Date", FieldType.Date, FieldSettings.Empty(), FractionalIndex.Create("a3"), Actor, Now, isSystem: true);
        var item = Item();

        item.UpdateFieldValue(due, FieldValue.Create(JsonValue.Create("\"2026-10-18T00:00:00.0000000+07:00\"")), Actor, Now.AddMinutes(1));

        item.FieldValues.Should().ContainSingle(v => v.FieldId == due.Id);
    }

    [Fact]
    public void AnInvalidOptionOnASystemStatusField_IsStillRejected()
    {
        var status = SystemStatus();
        var item = Item();

        var act = () => item.UpdateFieldValue(status, FieldValue.Create(JsonValue.Create($"\"{Guid.NewGuid()}\"")), Actor, Now.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_InvalidOptionValue);
        item.FieldValues.Should().BeEmpty();
    }

    [Fact]
    public void DeletingASystemField_IsStillRejected()
    {
        var status = SystemStatus();

        var act = () => status.Delete(Actor, Now.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_CannotDeleteSystem);
        status.IsDeleted.Should().BeFalse();
    }
}
