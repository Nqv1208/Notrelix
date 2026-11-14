using Notrelix.Application.Features.Collaboration.Notifications.ChatDigest;
using Notrelix.Application.Features.Collaboration.Public.Notifications;

namespace Notrelix.Application.Tests.Features.Chat.Notifications;

/// <summary>UC-CHT-H09: the window is one fixed UTC span, and the digest says what it lists, caps its lines with "and N more" and never injects markup.</summary>
public class ChatDigestMessageTests
{
    private static ChatDigestLine Line(string? space = "general", string? excerpt = "ping @Ana") =>
        new(space, ChatNotificationReasons.Mention, excerpt, "https://app.test/workspaces/w/chat/s");

    [Fact]
    public void TheWindowIsTheLatestCompletedSpanOfTheLength_AndTheKeyIsItsStart()
    {
        var now = new DateTimeOffset(2026, 11, 13, 9, 30, 0, TimeSpan.Zero);

        var window = ChatDigestWindow.Latest(now, TimeSpan.FromHours(24));

        window.Start.Should().Be(new DateTimeOffset(2026, 11, 12, 0, 0, 0, TimeSpan.Zero));
        window.End.Should().Be(new DateTimeOffset(2026, 11, 13, 0, 0, 0, TimeSpan.Zero));
        window.Key.Should().Be("202611120000");
        ChatDigestWindow.Latest(now.AddHours(10), TimeSpan.FromHours(24)).Should().Be(window, "every pass of the same day agrees");
    }

    [Fact]
    public void TheKeyNamesWorkspacePersonAndWindow()
    {
        var window = ChatDigestWindow.Latest(DateTimeOffset.UtcNow, TimeSpan.FromHours(1));
        var (workspace, user) = (Guid.NewGuid(), Guid.NewGuid());

        window.DeduplicationKey(workspace, user).Should().Be($"chat-digest:{workspace:N}:{user:N}:{window.Key}");
        window.DeduplicationKey(workspace, Guid.NewGuid()).Should().NotBe(window.DeduplicationKey(workspace, user));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10_080 + 1)]
    public void ALengthOutsideOneMinuteToSevenDaysIsRefused(int minutes)
    {
        var act = () => ChatDigestWindow.Latest(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(minutes));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ThreeMessagesAreOneEmailWithThreeLines()
    {
        var email = ChatDigestMessage.Render("en", [Line(), Line("design"), Line(null, "hello")], 0, "https://app.test/settings");

        email.Subject.Should().Be("You missed 3 chat messages");
        email.BodyText.Should().Contain("#general").And.Contain("#design").And.Contain("Direct message").And.Contain("https://app.test/settings");
        email.BodyText.Should().NotContain("more");
        email.BodyHtml.Should().Contain("<li>").And.Contain("href=\"https://app.test/workspaces/w/chat/s\"");
    }

    [Fact]
    public void MoreThanTheCapReadsAndNMore_AndTheSubjectCountsEverything()
    {
        var email = ChatDigestMessage.Render("en", [Line(), Line()], 7, "https://app.test/settings");

        email.Subject.Should().Be("You missed 9 chat messages");
        email.BodyText.Should().Contain("and 7 more");
        email.BodyHtml.Should().Contain("and 7 more");
    }

    [Fact]
    public void VietnameseIsUsedForAViLocale_AndAnyOtherLocaleReadsEnglish()
    {
        ChatDigestMessage.Render("vi-VN", [Line()], 0, "https://app.test/s").Subject.Should().Be("Bạn bỏ lỡ một tin nhắn trò chuyện");
        ChatDigestMessage.Render("fr", [Line()], 0, "https://app.test/s").Subject.Should().Be("You missed a chat message");
        ChatDigestMessage.Render(null, [Line()], 0, "https://app.test/s").Subject.Should().Be("You missed a chat message");
    }

    [Fact]
    public void AnExcerptIsShortenedAndEveryValueIsEncodedInTheHtml()
    {
        var email = ChatDigestMessage.Render("en", [Line("<b>x</b>", "<script>alert(1)</script> **bold** " + new string('a', 400))], 0, "https://app.test/s");

        email.BodyHtml.Should().NotContain("<script>").And.Contain("&lt;b&gt;x&lt;/b&gt;");
        email.BodyText.Should().NotContain("**").And.NotContain(new string('a', 300));
    }

    [Fact]
    public void ALineWithoutTextSaysSo()
    {
        ChatDigestMessage.Render("en", [Line(excerpt: null)], 0, "https://app.test/s").BodyText.Should().Contain("(no text)");
    }
}
