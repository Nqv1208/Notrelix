using FluentAssertions;
using Notrelix.Domain.Collaboration;
using Notrelix.Domain.Collaboration.Comments;

namespace Notrelix.Domain.Tests.Collaboration;

public class CommentAnchorTests
{
    [Fact]
    public void Create_WithSelectorAndOffset_ShouldSucceed()
    {
        var anchor = CommentAnchor.Create("div.content", 42);

        anchor.Selector.Should().Be("div.content");
        anchor.Offset.Should().Be(42);
    }

    [Fact]
    public void Create_WithDefaultValues_ShouldHaveNullProperties()
    {
        var anchor = CommentAnchor.Create();

        anchor.Selector.Should().BeNull();
        anchor.Offset.Should().BeNull();
    }

    [Fact]
    public void None_ShouldHaveNullProperties()
    {
        var anchor = CommentAnchor.None();

        anchor.Selector.Should().BeNull();
        anchor.Offset.Should().BeNull();
    }

    [Fact]
    public void None_ShouldBeSingleton()
    {
        var none1 = CommentAnchor.None();
        var none2 = CommentAnchor.None();

        none1.Should().Be(none2);
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var anchor1 = CommentAnchor.Create("selector", 5);
        var anchor2 = CommentAnchor.Create("selector", 5);

        anchor1.Should().Be(anchor2);
    }

    [Fact]
    public void Equality_DifferentSelector_ShouldNotBeEqual()
    {
        var anchor1 = CommentAnchor.Create("a", 1);
        var anchor2 = CommentAnchor.Create("b", 1);

        anchor1.Should().NotBe(anchor2);
    }

    [Fact]
    public void Equality_DifferentOffset_ShouldNotBeEqual()
    {
        var anchor1 = CommentAnchor.Create("x", 1);
        var anchor2 = CommentAnchor.Create("x", 2);

        anchor1.Should().NotBe(anchor2);
    }

    [Fact]
    public void ForTextRange_WithQuoteAsLongAsTheRange_ShouldKeepBlockQuoteAndOffsets()
    {
        var blockId = Guid.CreateVersion7();

        var anchor = CommentAnchor.ForTextRange(blockId, "world", 6, 11);

        anchor.IsTextRange.Should().BeTrue();
        anchor.BlockId.Should().Be(blockId);
        anchor.Quote.Should().Be("world");
        anchor.StartOffset.Should().Be(6);
        anchor.EndOffset.Should().Be(11);
        anchor.Selector.Should().BeNull();
    }

    [Fact]
    public void None_ShouldNotBeATextRange()
    {
        CommentAnchor.None().IsTextRange.Should().BeFalse();
    }

    [Fact]
    public void Equality_SameTextRange_ShouldBeEqual()
    {
        var blockId = Guid.CreateVersion7();

        CommentAnchor.ForTextRange(blockId, "ab", 1, 3).Should().Be(CommentAnchor.ForTextRange(blockId, "ab", 1, 3));
        CommentAnchor.ForTextRange(blockId, "ab", 1, 3).Should().NotBe(CommentAnchor.ForTextRange(blockId, "ab", 2, 4));
    }

    [Theory]
    [InlineData("", 0, 0)]
    [InlineData("   ", 0, 3)]
    [InlineData("abc", -1, 2)]
    [InlineData("abc", 3, 3)]
    [InlineData("abc", 5, 3)]
    [InlineData("abc", 0, 5)]
    [InlineData("abc", 20_000, 20_003)]
    public void ForTextRange_WithAnInvalidRangeOrQuote_ShouldBeRejected(string quote, int start, int end)
    {
        var act = () => CommentAnchor.ForTextRange(Guid.CreateVersion7(), quote, start, end);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(CollaborationRuleCodes.Collaboration_Comment_AnchorInvalid);
    }

    [Fact]
    public void ForTextRange_WithoutABlock_ShouldBeRejected()
    {
        var act = () => CommentAnchor.ForTextRange(Guid.Empty, "abc", 0, 3);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ForTextRange_WithAQuoteLongerThanTheLimit_ShouldBeRejected()
    {
        var quote = new string('x', CommentAnchor.MaxQuoteLength + 1);

        var act = () => CommentAnchor.ForTextRange(Guid.CreateVersion7(), quote, 0, quote.Length);

        act.Should().Throw<BusinessRuleException>();
    }
}
