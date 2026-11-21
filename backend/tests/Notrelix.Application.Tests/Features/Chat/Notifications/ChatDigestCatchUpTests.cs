using Notrelix.Application.Features.Collaboration.Notifications.ChatDigest;

namespace Notrelix.Application.Tests.Features.Chat.Notifications;

/// <summary>UC-CHT-H09: the windows a pass covers, so a window the job missed is sent on the next pass.</summary>
public class ChatDigestCatchUpTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 30, 0, TimeSpan.Zero);
    private static readonly TimeSpan Day = TimeSpan.FromHours(24);

    [Fact]
    public void A_pass_covers_the_missed_windows_oldest_first_and_ends_with_the_latest()
    {
        var windows = ChatDigestWindow.Recent(Now, Day, catchUp: 2);

        windows.Should().HaveCount(3);
        windows[^1].Should().Be(ChatDigestWindow.Latest(Now, Day));
        windows[0].End.Should().Be(windows[1].Start);
        windows[1].End.Should().Be(windows[2].Start);
        windows[0].Start.Should().Be(new DateTimeOffset(2026, 11, 9, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void No_catch_up_is_the_latest_window_only()
    {
        ChatDigestWindow.Recent(Now, Day, catchUp: 0).Should().Equal(ChatDigestWindow.Latest(Now, Day));
    }
}
