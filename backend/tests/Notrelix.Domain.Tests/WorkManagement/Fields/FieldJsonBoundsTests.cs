using FluentAssertions;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Domain.Tests.WorkManagement.Fields;

/// <summary>WM-V3-QUAL-001 / SPEC WMREQ146: flexible field JSON is bounded in size and depth.</summary>
public class FieldJsonBoundsTests
{
    private static string Nested(int depth) => new string('[', depth) + new string(']', depth);

    /// <summary>A JSON object nested <paramref name="depth"/> levels deep.</summary>
    private static string NestedObject(int depth) =>
        string.Concat(Enumerable.Repeat("{\"a\":", depth - 1)) + "{}" + new string('}', depth - 1);

    [Fact]
    public void Settings_AtTheBounds_AreAccepted()
    {
        var settings = FieldSettings.Create(JsonValue.Create(NestedObject(FieldJsonBounds.MaxDepth)));

        var act = () => FieldSettingsValidator.Validate(settings, FieldType.Text);

        act.Should().NotThrow();
    }

    [Fact]
    public void Settings_TooDeep_AreRejected()
    {
        var settings = FieldSettings.Create(JsonValue.Create(NestedObject(FieldJsonBounds.MaxDepth + 1)));

        var act = () => FieldSettingsValidator.Validate(settings, FieldType.Text);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_FieldSettings_TooLarge);
    }

    [Fact]
    public void Settings_TooLarge_AreRejected()
    {
        var settings = FieldSettings.Create(JsonValue.Create($"{{\"note\":\"{new string('x', FieldJsonBounds.MaxSettingsLength)}\"}}"));

        var act = () => FieldSettingsValidator.Validate(settings, FieldType.Text);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_FieldSettings_TooLarge);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Value_BeyondTheBounds_IsRejected(bool tooLarge)
    {
        var json = tooLarge ? $"\"{new string('x', FieldJsonBounds.MaxValueLength)}\"" : Nested(FieldJsonBounds.MaxDepth + 1);

        var act = () => FieldValueValidator.Validate(FieldValue.Create(JsonValue.Create(json)), FieldType.LongText, FieldSettings.Empty());

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_FieldValue_TooLarge);
    }
}
