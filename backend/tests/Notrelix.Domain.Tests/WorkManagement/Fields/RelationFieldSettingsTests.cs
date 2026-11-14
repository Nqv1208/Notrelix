using FluentAssertions;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Domain.Tests.WorkManagement.Fields;

public class RelationFieldSettingsTests
{
    private static readonly Guid Target = Guid.Parse("6f1c2a3e-0000-4000-8000-0000000000b0");

    [Fact]
    public void Settings_WithoutTargetBoard_AreRefused()
    {
        Action act = () => FieldSettingsValidator.Validate(FieldSettings.Empty(), FieldType.Relation);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Relation_TargetBoardRequired);
    }

    [Fact]
    public void Settings_WithTargetBoard_AreAccepted()
    {
        var settings = FieldSettings.Create(JsonValue.Create($"{{\"targetBoardId\":\"{Target}\"}}"));

        Action act = () => FieldSettingsValidator.Validate(settings, FieldType.Relation);

        act.Should().NotThrow();
        RelationFieldSettings.TargetBoardId(settings.Data.Value).Should().Be(Target);
    }

    [Theory]
    [InlineData("{\"targetBoardId\":\"not-a-guid\"}")]
    [InlineData("{\"targetBoardId\":\"00000000-0000-0000-0000-000000000000\"}")]
    [InlineData("{\"other\":1}")]
    public void TargetBoardId_IsNullWhenNotAUsableGuid(string settings)
    {
        RelationFieldSettings.TargetBoardId(settings).Should().BeNull();
    }

    [Fact]
    public void Value_LinksItemsByCanonicalSortedSet_AndRefusesDuplicates()
    {
        var a = Guid.Parse("6f1c2a3e-0000-4000-8000-0000000000a1");
        var b = Guid.Parse("6f1c2a3e-0000-4000-8000-0000000000a2");

        Action duplicate = () => FieldValueValidator.Validate(
            FieldValue.Create(JsonValue.Create($"[\"{a}\",\"{a}\"]")), FieldType.Relation, FieldSettings.Empty());
        Action links = () => FieldValueValidator.Validate(
            FieldValue.Create(JsonValue.Create($"[\"{a}\",\"{b}\"]")), FieldType.Relation, FieldSettings.Empty());

        duplicate.Should().Throw<BusinessRuleException>();
        links.Should().NotThrow();
    }

    [Fact]
    public void Value_OverTheCap_IsRefused()
    {
        var ids = Enumerable.Range(1, RelationFieldSettings.MaxLinks + 1).Select(_ => $"\"{Guid.NewGuid()}\"");
        Action act = () => FieldValueValidator.Validate(
            FieldValue.Create(JsonValue.Create($"[{string.Join(",", ids)}]")), FieldType.Relation, FieldSettings.Empty());

        act.Should().Throw<BusinessRuleException>().WithMessage("*at most*");
    }
}
