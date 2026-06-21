using FluentAssertions;
using Notrelix.Domain.Collaboration.Comments;
using Xunit;

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
}
