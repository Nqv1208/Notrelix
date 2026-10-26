using Notrelix.Application.Features.Collaboration.Notifications;

namespace Notrelix.Application.Tests.Features.Collaboration;

public class NotificationExcerptTests
{
    [Fact]
    public void Excerpt_IsPlainTextOnOneLine()
    {
        var excerpt = NotificationExcerpt.From("## Heading\n**Hi** [Ana](https://example.test/ana), <b>please</b>\n\n review `this`");

        excerpt.Should().Be("Heading Hi Ana, please review this");
    }

    [Fact]
    public void Excerpt_IsCutAtTwoHundredCharactersWithAnEllipsis()
    {
        var excerpt = NotificationExcerpt.From(new string('a', 500));

        excerpt.Should().HaveLength(NotificationExcerpt.Length);
        excerpt.Should().EndWith("…");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("<br/>")]
    public void Excerpt_IsNullWhenNothingReadableRemains(string? text)
    {
        NotificationExcerpt.From(text).Should().BeNull();
    }

    [Fact]
    public void Excerpt_KeepsSnakeCaseWordsAndHashtagsInsideALine()
    {
        NotificationExcerpt.From("fix my_var in #123").Should().Be("fix my_var in #123");
    }
}
