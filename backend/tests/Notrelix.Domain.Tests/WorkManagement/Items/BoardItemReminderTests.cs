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
}
