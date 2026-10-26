using FluentAssertions;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Rules;

namespace Notrelix.Domain.Tests.Documents;

public class BlockPropertiesValidatorTests
{
    private static string Normalised(BlockType type, string json) =>
        BlockPropertiesValidator.Normalize(type, BlockProperties.Create(JsonValue.Create(json))).Data.Value;

    private static BusinessRuleException Rejected(BlockType type, string json)
    {
        var act = () => BlockPropertiesValidator.Normalize(type, BlockProperties.Create(JsonValue.Create(json)));
        return act.Should().Throw<BusinessRuleException>().Which;
    }

    private static readonly Guid Id = Guid.NewGuid();

    [Theory]
    [InlineData(BlockType.Text)]
    [InlineData(BlockType.Heading1)]
    [InlineData(BlockType.BulletList)]
    [InlineData(BlockType.ToDo)]
    [InlineData(BlockType.Quote)]
    [InlineData(BlockType.Code)]
    [InlineData(BlockType.Callout)]
    [InlineData(BlockType.Toggle)]
    public void TextLikeType_AcceptsAlignment(BlockType type)
    {
        Normalised(type, "{\"align\":\"justify\"}").Should().Be("{\"align\":\"justify\"}");
    }

    [Theory]
    [InlineData("middle")]
    [InlineData("LEFT")]
    public void Alignment_OutsideTheSet_IsRejected(string align)
    {
        Rejected(BlockType.Text, $"{{\"align\":\"{align}\"}}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
    }

    [Theory]
    [InlineData(BlockType.Text, "{\"checked\":true}")]
    [InlineData(BlockType.Heading2, "{\"language\":\"c\"}")]
    [InlineData(BlockType.ToDo, "{\"collapsed\":true}")]
    [InlineData(BlockType.Divider, "{\"align\":\"left\"}")]
    [InlineData(BlockType.Image, "{\"align\":\"left\"}")]
    [InlineData(BlockType.Text, "{\"color\":\"red\"}")]
    [InlineData(BlockType.Text, "{\"text\":\"legacy\"}")]
    public void PropertyTheTypeDoesNotHave_IsRejected(BlockType type, string json)
    {
        Rejected(type, json).RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("\"x\"")]
    [InlineData("null")]
    public void NonObjectProperties_AreRejected(string json)
    {
        Rejected(BlockType.Text, json).RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
    }

    [Fact]
    public void ToDo_Checked_MustBeABoolean()
    {
        Normalised(BlockType.ToDo, "{\"checked\":false}").Should().Be("{\"checked\":false}");
        Rejected(BlockType.ToDo, "{\"checked\":\"yes\"}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
        Rejected(BlockType.ToDo, "{\"checked\":null}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
    }

    [Fact]
    public void Toggle_Collapsed_MustBeABoolean()
    {
        Normalised(BlockType.Toggle, "{\"collapsed\":true}").Should().Be("{\"collapsed\":true}");
        Rejected(BlockType.Toggle, "{\"collapsed\":1}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
    }

    [Theory]
    [InlineData("csharp", "csharp")]
    [InlineData("C#", "c#")]
    [InlineData("objective-c", "objective-c")]
    public void Code_Language_IsLowercased(string language, string expected)
    {
        Normalised(BlockType.Code, $"{{\"language\":\"{language}\"}}").Should().Be($"{{\"language\":\"{expected}\"}}");
    }

    [Theory]
    [InlineData("")]
    [InlineData("type script")]
    [InlineData("a_b")]
    [InlineData("x/y")]
    public void Code_Language_WithOtherCharacters_IsRejected(string language)
    {
        Rejected(BlockType.Code, $"{{\"language\":\"{language}\"}}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
    }

    [Fact]
    public void Code_Language_LongerThan32_IsRejected()
    {
        Rejected(BlockType.Code, $"{{\"language\":\"{new string('a', 33)}\"}}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
        Normalised(BlockType.Code, $"{{\"language\":\"{new string('a', 32)}\"}}").Should().Contain(new string('a', 32));
    }

    [Theory]
    [InlineData("info")]
    [InlineData("success")]
    [InlineData("warning")]
    [InlineData("danger")]
    [InlineData("neutral")]
    public void Callout_Tone_IsAccepted(string tone)
    {
        Normalised(BlockType.Callout, $"{{\"icon\":\"!\",\"tone\":\"{tone}\"}}").Should().Be($"{{\"icon\":\"!\",\"tone\":\"{tone}\"}}");
    }

    [Fact]
    public void Callout_RejectsAnUnknownToneAndABadIcon()
    {
        Rejected(BlockType.Callout, "{\"tone\":\"loud\"}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
        Rejected(BlockType.Callout, "{\"icon\":\"\"}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
        Rejected(BlockType.Callout, $"{{\"icon\":\"{new string('i', 17)}\"}}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
    }

    [Fact]
    public void Image_AcceptsItsProperties_InCanonicalOrder()
    {
        Normalised(BlockType.Image, $"{{\"width\":640,\"alt\":\"A cat\",\"attachmentId\":\"{Id.ToString().ToUpperInvariant()}\"}}")
            .Should().Be($"{{\"alt\":\"A cat\",\"attachmentId\":\"{Id}\",\"width\":640}}");
    }

    [Theory]
    [InlineData(79)]
    [InlineData(2001)]
    [InlineData(0)]
    [InlineData(-5)]
    public void Image_Width_OutOfRange_IsRejected(int width)
    {
        Rejected(BlockType.Image, $"{{\"width\":{width}}}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
    }

    [Theory]
    [InlineData(80)]
    [InlineData(2000)]
    public void Image_Width_AtTheBounds_IsAccepted(int width)
    {
        Normalised(BlockType.Image, $"{{\"width\":{width}}}").Should().Be($"{{\"width\":{width}}}");
    }

    [Fact]
    public void Image_Width_MustBeAWholeNumber()
    {
        Rejected(BlockType.Image, "{\"width\":100.5}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
        Rejected(BlockType.Image, "{\"width\":\"100\"}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
    }

    [Fact]
    public void Image_Alt_LongerThan500_IsRejected()
    {
        Rejected(BlockType.Image, $"{{\"alt\":\"{new string('a', 501)}\"}}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
    }

    [Fact]
    public void Image_Caption_IsNormalisedRuns()
    {
        Normalised(BlockType.Image, "{\"caption\":[{\"text\":\"a\",\"marks\":[{\"type\":\"bold\"}]},{\"text\":\"b\",\"marks\":[{\"type\":\"bold\"}]},{\"text\":\"\",\"marks\":[]}]}")
            .Should().Be("{\"caption\":[{\"text\":\"ab\",\"marks\":[{\"type\":\"bold\"}]}]}");
        Rejected(BlockType.Image, "{\"caption\":[{\"text\":\"a\",\"marks\":[{\"type\":\"link\",\"href\":\"javascript:x\"}]}]}")
            .RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_LinkHrefInvalid);
    }

    [Theory]
    [InlineData(BlockType.Image, "attachmentId")]
    [InlineData(BlockType.File, "attachmentId")]
    [InlineData(BlockType.Video, "attachmentId")]
    [InlineData(BlockType.Board, "boardId")]
    [InlineData(BlockType.Board, "viewId")]
    public void IdProperty_MustBeAGuid(BlockType type, string key)
    {
        Normalised(type, $"{{\"{key}\":\"{Id}\"}}").Should().Be($"{{\"{key}\":\"{Id}\"}}");
        Rejected(type, $"{{\"{key}\":\"nope\"}}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
        Rejected(type, $"{{\"{key}\":\"{Guid.Empty}\"}}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
        Rejected(type, $"{{\"{key}\":5}}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
    }

    [Fact]
    public void PageReference_PageId_MustBeAGuid()
    {
        Normalised(BlockType.PageReference, $"{{\"pageId\":\"{Id}\"}}").Should().Be($"{{\"pageId\":\"{Id}\"}}");
        Rejected(BlockType.PageReference, "{\"pageId\":\"x\"}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PageReferenceTargetInvalid);
    }

    [Fact]
    public void Divider_HasNoProperties()
    {
        Normalised(BlockType.Divider, "{}").Should().Be("{}");
        Rejected(BlockType.Divider, "{\"x\":1}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
    }

    [Fact]
    public void Table_AcceptsRowsHeaderAndWidths()
    {
        var json = "{\"colWidths\":[120,null],\"headerRow\":true,\"rows\":[[{\"bg\":\"muted\",\"runs\":[{\"text\":\"A\",\"marks\":[{\"type\":\"bold\"}]}]},{\"runs\":[]}]]}";

        Normalised(BlockType.Table, json).Should().Be(json);
    }

    [Fact]
    public void Table_CellRuns_AreNormalised()
    {
        Normalised(BlockType.Table, "{\"rows\":[[{\"runs\":[{\"text\":\"a\",\"marks\":[]},{\"text\":\"b\",\"marks\":[]}]}]]}")
            .Should().Be("{\"rows\":[[{\"runs\":[{\"text\":\"ab\",\"marks\":[]}]}]]}");
    }

    [Fact]
    public void Table_At50Columns_And500Rows_IsAccepted()
    {
        var cell = "{\"runs\":[]}";
        var row = "[" + string.Join(",", Enumerable.Repeat(cell, 50)) + "]";
        var rows = "[" + string.Join(",", Enumerable.Repeat(row, 500)) + "]";

        var act = () => Normalised(BlockType.Table, $"{{\"rows\":{rows}}}");

        act.Should().NotThrow();
    }

    [Fact]
    public void Table_Over50Columns_IsRejected()
    {
        var row = "[" + string.Join(",", Enumerable.Repeat("{\"runs\":[]}", 51)) + "]";

        Rejected(BlockType.Table, $"{{\"rows\":[{row}]}}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
    }

    [Fact]
    public void Table_Over500Rows_IsRejected()
    {
        var rows = "[" + string.Join(",", Enumerable.Repeat("[]", 501)) + "]";

        Rejected(BlockType.Table, $"{{\"rows\":{rows}}}").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
    }

    [Theory]
    [InlineData("{\"rows\":\"x\"}")]
    [InlineData("{\"rows\":[\"x\"]}")]
    [InlineData("{\"rows\":[[\"x\"]]}")]
    [InlineData("{\"rows\":[[{\"runs\":[],\"w\":1}]]}")]
    [InlineData("{\"rows\":[[{\"runs\":[],\"bg\":\"red\"}]]}")]
    [InlineData("{\"headerRow\":\"yes\"}")]
    [InlineData("{\"colWidths\":\"x\"}")]
    [InlineData("{\"colWidths\":[5]}")]
    public void Table_Malformed_IsRejected(string json)
    {
        Rejected(BlockType.Table, json).RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PropertiesInvalid);
    }

    [Fact]
    public void Convert_KeepsOnlyWhatTheNewTypeHas()
    {
        var converted = BlockPropertiesValidator.Convert(BlockType.Quote,
            BlockProperties.Create(JsonValue.Create("{\"align\":\"right\",\"checked\":true,\"language\":\"c\"}")));

        converted.Data.Value.Should().Be("{\"align\":\"right\"}");
    }
}
