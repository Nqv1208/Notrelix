using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Domain.Tests.WorkManagement.Items;

public class BoardItemReminderTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

    private static BoardItemReminder Reminder() =>
        BoardItemReminder.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Start);

    [Fact]
    public void Create_RecordsWhenItWasSentAndWhenTheNextMayBe()
    {
        var reminder = Reminder();

        reminder.RemindedAt.Should().Be(Start);
        reminder.NextRemindAt.Should().Be(Start.AddHours(24));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(23)]
    public void Remind_BeforeTheLimitEnds_IsRejectedAndLeavesTheStateUnchanged(int hours)
    {
        var reminder = Reminder();

        var act = () => reminder.Remind(Start.AddHours(hours));

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Reminder_TooSoon);
        reminder.RemindedAt.Should().Be(Start);
    }

    [Fact]
    public void Remind_WhenTheLimitEnds_MovesTheNextMomentForward()
    {
        var reminder = Reminder();
        var later = Start.AddHours(24);

        reminder.CanRemind(later).Should().BeTrue();
        reminder.Remind(later);

        reminder.RemindedAt.Should().Be(later);
        reminder.NextRemindAt.Should().Be(later.AddHours(24));
    }

    [Fact]
    public void Create_RejectsEmptyIdentities()
    {
        var act = () => BoardItemReminder.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Start);

        act.Should().Throw<BusinessRuleException>();
    }
}
