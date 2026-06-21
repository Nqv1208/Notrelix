using FluentAssertions;
using Notrelix.Domain.Collaboration.Reactions;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

namespace Notrelix.Domain.Tests.Collaboration;

public class EmojiTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var emoji = Emoji.Create("+1");

        emoji.Code.Should().Be("+1");
    }

    [Fact]
    public void Create_ShouldTrimCode()
    {
        var emoji = Emoji.Create("  heart  ");

        emoji.Code.Should().Be("heart");
    }

    [Fact]
    public void Create_WithEmptyCode_ShouldThrow()
    {
        var act = () => Emoji.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithWhiteSpaceCode_ShouldThrow()
    {
        var act = () => Emoji.Create("   ");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameCode_ShouldBeEqual()
    {
        var emoji1 = Emoji.Create("rocket");
        var emoji2 = Emoji.Create("rocket");

        emoji1.Should().Be(emoji2);
    }

    [Fact]
    public void Equality_DifferentCode_ShouldNotBeEqual()
    {
        var emoji1 = Emoji.Create("+1");
        var emoji2 = Emoji.Create("heart");

        emoji1.Should().NotBe(emoji2);
    }
}
