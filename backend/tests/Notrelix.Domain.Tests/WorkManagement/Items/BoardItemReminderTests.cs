using FluentAssertions;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Domain.Tests.WorkManagement.Items;

public class BoardItemReminderTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NextAfter_IsADayLater()
    {
        BoardItemReminder.NextAfter(Start).Should().Be(Start.AddHours(24));
    }

    [Fact]
    public void WindowOf_IsSharedByARetryAndNeverByTwoRemindersAtLeastADayApart()
    {
        BoardItemReminder.WindowOf(Start).Should().Be(BoardItemReminder.WindowOf(Start.AddSeconds(30)));

        foreach (var offset in new[] { 0, 1, 7, 13, 23 })
        {
            var first = Start.AddHours(offset).AddMinutes(17);
            BoardItemReminder.WindowOf(first).Should().NotBe(BoardItemReminder.WindowOf(first.Add(BoardItemReminder.Cooldown)));
        }
    }
}
