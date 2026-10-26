using FluentAssertions;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Rules;

namespace Notrelix.Domain.Tests.Documents;

public class BlockContentValidatorTests
{
    private static string Normalised(BlockType type, string json) =>
        BlockContentValidator.Normalize(type, BlockContent.Create(JsonValue.Create(json))).Data.Value;

    private static BusinessRuleException Rejected(BlockType type, string json)
    {
        var act = () => BlockContentValidator.Normalize(type, BlockContent.Create(JsonValue.Create(json)));
        return act.Should().Throw<BusinessRuleException>().Which;
    }

    private static string Run(string text, string marks = "") =>
        $"{{\"text\":\"{text}\",\"marks\":[{marks}]}}";

    private static string Doc(params string[] runs) => $"{{\"v\":1,\"runs\":[{string.Join(",", runs)}]}}";

    public static TheoryData<BlockType> TextLikeTypes => new()
    {
        BlockType.Text, BlockType.Heading1, BlockType.Heading2, BlockType.Heading3, BlockType.BulletList,
        BlockType.NumberedList, BlockType.ToDo, BlockType.Toggle, BlockType.Quote, BlockType.Callout, BlockType.Code,
    };

    public static TheoryData<BlockType> NonTextTypes => new()
    {
        BlockType.Divider, BlockType.Image, BlockType.File, BlockType.Video, BlockType.Board, BlockType.Table, BlockType.PageReference,
    };

    [Theory]
    [MemberData(nameof(TextLikeTypes))]
    public void TextLikeType_AcceptsRichTextV1(BlockType type)
    {
        Normalised(type, Doc(Run("Hello"))).Should().Be(Doc(Run("Hello")));
    }

    [Theory]
    [MemberData(nameof(TextLikeTypes))]
    public void TextLikeType_AnEmptyObjectIsAnEmptyBlock(BlockType type)
    {
        Normalised(type, "{}").Should().Be("{\"v\":1,\"runs\":[]}");
    }

    [Theory]
    [MemberData(nameof(NonTextTypes))]
    public void NonTextType_AcceptsAnEmptyContentObject(BlockType type)
    {
        Normalised(type, "{}").Should().Be("{}");
    }

    [Theory]
    [MemberData(nameof(NonTextTypes))]
    public void NonTextType_RejectsAnyContent(BlockType type)
    {
        Rejected(type, Doc(Run("x"))).RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_ContentInvalid);
    }

    [Fact]
    public void NullContent_IsRejected()
    {
        Rejected(BlockType.Text, "null").RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_ContentCannotBeNull);
    }

    [Theory]
    [InlineData("\"just a string\"")]
    [InlineData("[]")]
    [InlineData("42")]
    [InlineData("{\"text\":\"legacy flat shape\"}")]
    [InlineData("{\"v\":2,\"runs\":[]}")]
    [InlineData("{\"v\":1}")]
    [InlineData("{\"runs\":[]}")]
    [InlineData("{\"v\":1,\"runs\":[],\"extra\":true}")]
    [InlineData("{\"v\":1,\"runs\":\"x\"}")]
    [InlineData("{\"v\":1,\"runs\":[\"x\"]}")]
    [InlineData("{\"v\":1,\"runs\":[{\"marks\":[]}]}")]
    [InlineData("{\"v\":1,\"runs\":[{\"text\":5,\"marks\":[]}]}")]
    [InlineData("{\"v\":1,\"runs\":[{\"text\":\"a\",\"marks\":[],\"x\":1}]}")]
    [InlineData("{\"v\":1,\"runs\":[{\"text\":\"a\",\"marks\":{}}]}")]
    public void MalformedContent_IsRejected(string json)
    {
        var rejection = Rejected(BlockType.Text, json);
        rejection.RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_ContentInvalid);
    }

    [Theory]
    [InlineData("bold")]
    [InlineData("italic")]
    [InlineData("underline")]
    [InlineData("strike")]
    [InlineData("code")]
    public void SimpleMark_IsAccepted(string type)
    {
        var json = Doc(Run("x", $"{{\"type\":\"{type}\"}}"));
        Normalised(BlockType.Text, json).Should().Be(json);
    }

    [Theory]
    [InlineData("{\"type\":\"bold\",\"extra\":1}")]
    [InlineData("{\"type\":\"sparkle\"}")]
    [InlineData("{\"type\":\"link\"}")]
    [InlineData("{\"type\":\"link\",\"href\":5}")]
    [InlineData("{\"type\":\"color\",\"token\":\"pink\"}")]
    [InlineData("{\"type\":\"highlight\",\"token\":\"default\"}")]
    [InlineData("{\"type\":\"color\",\"token\":\"none\"}")]
    [InlineData("{\"type\":\"mention\",\"userId\":\"not-a-guid\"}")]
    [InlineData("{\"type\":\"pageRef\",\"pageId\":\"00000000-0000-0000-0000-000000000000\"}")]
    [InlineData("{\"type\":\"bold\",\"href\":\"https://x.io\"}")]
    [InlineData("{}")]
    [InlineData("\"bold\"")]
    public void UnknownOrMalformedMark_IsRejected(string mark)
    {
        Rejected(BlockType.Text, Doc(Run("x", mark))).RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_MarkInvalid);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("muted")]
    [InlineData("primary")]
    [InlineData("accent")]
    [InlineData("success")]
    [InlineData("warning")]
    [InlineData("danger")]
    public void ColorToken_IsAccepted(string token)
    {
        var json = Doc(Run("x", $"{{\"type\":\"color\",\"token\":\"{token}\"}}"));
        Normalised(BlockType.Text, json).Should().Be(json);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("muted")]
    [InlineData("accent")]
    [InlineData("primary")]
    [InlineData("success")]
    [InlineData("warning")]
    [InlineData("danger")]
    public void HighlightToken_IsAccepted(string token)
    {
        var json = Doc(Run("x", $"{{\"type\":\"highlight\",\"token\":\"{token}\"}}"));
        Normalised(BlockType.Text, json).Should().Be(json);
    }

    [Theory]
    [InlineData("superscript")]
    [InlineData("subscript")]
    public void RaisedAndLoweredText_AreAccepted(string mark)
    {
        var json = Doc(Run("x", $"{{\"type\":\"{mark}\"}}"));
        Normalised(BlockType.Text, json).Should().Be(json);
    }

    [Fact]
    public void RaisedAndLoweredText_ExcludeEachOther_RaisedWins()
    {
        var json = Doc(Run("x", "{\"type\":\"subscript\"},{\"type\":\"superscript\"}"));
        Normalised(BlockType.Text, json).Should().Be(Doc(Run("x", "{\"type\":\"superscript\"}")));
    }

    [Fact]
    public void RaisedText_WithAnAttribute_IsRejected()
    {
        Rejected(BlockType.Text, Doc(Run("x", "{\"type\":\"superscript\",\"token\":\"muted\"}")))
            .RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_MarkInvalid);
    }

    [Fact]
    public void MentionAndPageRef_AcceptIds_AndNormaliseTheirCase()
    {
        var user = Guid.NewGuid();
        var page = Guid.NewGuid();
        var json = Doc(Run("@Ann", $"{{\"type\":\"mention\",\"userId\":\"{user.ToString().ToUpperInvariant()}\"}}"),
                       Run("Spec", $"{{\"type\":\"pageRef\",\"pageId\":\"{page}\"}}"));

        Normalised(BlockType.Text, json).Should().Be(Doc(
            Run("@Ann", $"{{\"type\":\"mention\",\"userId\":\"{user}\"}}"),
            Run("Spec", $"{{\"type\":\"pageRef\",\"pageId\":\"{page}\"}}")));
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/a?b=c#d")]
    [InlineData("HTTPS://Example.com")]
    [InlineData("mailto:someone@example.com")]
    public void Link_WithASafeHref_IsAccepted(string href)
    {
        var json = Doc(Run("x", $"{{\"type\":\"link\",\"href\":\"{href}\"}}"));
        Normalised(BlockType.Text, json).Should().Be(json);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JaVaScRiPt:alert(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com")]
    [InlineData("/relative/path")]
    [InlineData("example.com")]
    [InlineData("//example.com")]
    [InlineData("https://")]
    [InlineData("mailto:")]
    [InlineData("")]
    [InlineData("https://exa mple.com")]
    [InlineData("java\\tscript:alert(1)")]
    public void Link_WithAnUnsafeHref_IsRejected(string href)
    {
        var rejection = Rejected(BlockType.Text, Doc(Run("x", $"{{\"type\":\"link\",\"href\":\"{href}\"}}")));
        rejection.RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_LinkHrefInvalid);
    }

    [Fact]
    public void Link_LongerThan2048_IsRejected()
    {
        var href = "https://example.com/" + new string('a', 2048);

        Rejected(BlockType.Text, Doc(Run("x", $"{{\"type\":\"link\",\"href\":\"{href}\"}}")))
            .RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_LinkHrefInvalid);
    }

    [Fact]
    public void Link_Of2048Characters_IsAccepted()
    {
        var href = "https://example.com/" + new string('a', 2048 - "https://example.com/".Length);

        var act = () => Normalised(BlockType.Text, Doc(Run("x", $"{{\"type\":\"link\",\"href\":\"{href}\"}}")));

        act.Should().NotThrow();
    }

    [Fact]
    public void Link_HrefIsTrimmed()
    {
        Normalised(BlockType.Text, Doc(Run("x", "{\"type\":\"link\",\"href\":\"  https://example.com  \"}")))
            .Should().Be(Doc(Run("x", "{\"type\":\"link\",\"href\":\"https://example.com\"}")));
    }

    [Fact]
    public void Normalisation_DropsEmptyRuns()
    {
        Normalised(BlockType.Text, Doc(Run(""), Run("a"), Run("", "{\"type\":\"bold\"}")))
            .Should().Be(Doc(Run("a")));
    }

    [Fact]
    public void Normalisation_MergesAdjacentRunsWithTheSameMarks()
    {
        Normalised(BlockType.Text, Doc(
                Run("a", "{\"type\":\"bold\"},{\"type\":\"italic\"}"),
                Run("b", "{\"type\":\"italic\"},{\"type\":\"bold\"}"),
                Run("c"),
                Run("d", "{\"type\":\"bold\"}")))
            .Should().Be(Doc(
                Run("ab", "{\"type\":\"bold\"},{\"type\":\"italic\"}"),
                Run("c"),
                Run("d", "{\"type\":\"bold\"}")));
    }

    [Fact]
    public void Normalisation_DoesNotMergeRunsThatDifferInAMarkValue()
    {
        var json = Doc(
            Run("a", "{\"type\":\"link\",\"href\":\"https://a.io\"}"),
            Run("b", "{\"type\":\"link\",\"href\":\"https://b.io\"}"));

        Normalised(BlockType.Text, json).Should().Be(json);
    }

    [Fact]
    public void Normalisation_SortsMarksByType()
    {
        Normalised(BlockType.Text, Doc(Run("a", "{\"type\":\"underline\"},{\"type\":\"strike\"},{\"type\":\"italic\"},{\"type\":\"bold\"}")))
            .Should().Be(Doc(Run("a", "{\"type\":\"bold\"},{\"type\":\"italic\"},{\"type\":\"strike\"},{\"type\":\"underline\"}")));
    }

    [Fact]
    public void Normalisation_CollapsesDuplicateMarkTypes_LastWins()
    {
        Normalised(BlockType.Text, Doc(Run("a", "{\"type\":\"color\",\"token\":\"danger\"},{\"type\":\"bold\"},{\"type\":\"color\",\"token\":\"success\"}")))
            .Should().Be(Doc(Run("a", "{\"type\":\"bold\"},{\"type\":\"color\",\"token\":\"success\"}")));
    }

    [Fact]
    public void Normalisation_CodeExcludesEveryMarkButLink()
    {
        Normalised(BlockType.Text, Doc(Run("a", "{\"type\":\"bold\"},{\"type\":\"code\"},{\"type\":\"color\",\"token\":\"danger\"},{\"type\":\"link\",\"href\":\"https://a.io\"}")))
            .Should().Be(Doc(Run("a", "{\"type\":\"code\"},{\"type\":\"link\",\"href\":\"https://a.io\"}")));
    }

    [Fact]
    public void CodeBlock_RunsCarryNoMarks()
    {
        Normalised(BlockType.Code, Doc(Run("let x", "{\"type\":\"bold\"}"), Run(" = 1")))
            .Should().Be(Doc(Run("let x = 1")));
    }

    [Fact]
    public void NewLine_IsKeptInsideTheText()
    {
        Normalised(BlockType.Text, Doc(Run("a\\nb"))).Should().Be(Doc(Run("a\\nb")));
    }

    [Fact]
    public void Text_AtTheLimit_IsAccepted()
    {
        var act = () => Normalised(BlockType.Text, Doc(Run(new string('a', 20_000))));
        act.Should().NotThrow();
    }

    [Fact]
    public void Text_OverTheLimit_IsRejected()
    {
        Rejected(BlockType.Text, Doc(Run(new string('a', 20_001))))
            .RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_ContentTooLarge);
    }

    [Fact]
    public void Text_OverTheLimitAcrossRuns_IsRejected()
    {
        Rejected(BlockType.Text, Doc(Run(new string('a', 10_000)), Run(new string('b', 10_001), "{\"type\":\"bold\"}")))
            .RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_ContentTooLarge);
    }

    [Fact]
    public void Text_WithANulCharacter_IsRejected()
    {
        Rejected(BlockType.Text, Doc(Run("a\\u0000b")))
            .RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_ContentInvalid);
    }

    [Fact]
    public void TooManyRuns_IsRejected()
    {
        var runs = Enumerable.Range(0, 5_001).Select(i => Run("a", i % 2 == 0 ? "{\"type\":\"bold\"}" : "")).ToArray();

        Rejected(BlockType.Text, Doc(runs)).RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_ContentTooLarge);
    }

    [Fact]
    public void AreSame_IgnoresMemberOrderOfTheStoredJson()
    {
        var canonical = BlockContentValidator.Normalize(BlockType.Text, BlockContent.Create(JsonValue.Create(
            Doc(Run("a", "{\"type\":\"link\",\"href\":\"https://a.io\"}")))));
        // jsonb hands members back shortest key first, so "href" precedes "type".
        var stored = BlockContent.Create(JsonValue.Create(
            "{\"v\":1,\"runs\":[{\"text\":\"a\",\"marks\":[{\"href\":\"https://a.io\",\"type\":\"link\"}]}]}"));

        BlockContentValidator.AreSame(BlockType.Text, stored, canonical).Should().BeTrue();
    }

    [Fact]
    public void AreSame_IsFalseForContentThatWasNeverUpgraded()
    {
        var stored = BlockContent.Create(JsonValue.Create("{\"text\":\"legacy\"}"));
        var candidate = BlockContent.Create(JsonValue.Create("{\"v\":1,\"runs\":[]}"));

        BlockContentValidator.AreSame(BlockType.Text, stored, candidate).Should().BeFalse();
    }
}
