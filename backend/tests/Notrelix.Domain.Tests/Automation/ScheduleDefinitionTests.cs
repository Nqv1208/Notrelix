using FluentAssertions;
using Notrelix.Domain.Automation.Scheduled;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

namespace Notrelix.Domain.Tests.Automation;

public class ScheduleDefinitionTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var schedule = ScheduleDefinition.Create("0 9 * * 1-5");

        schedule.CronExpression.Should().Be("0 9 * * 1-5");
        schedule.TimeZone.Should().Be("UTC");
    }

    [Fact]
    public void Create_WithCustomTimeZone_ShouldSucceed()
    {
        var schedule = ScheduleDefinition.Create("0 0 * * *", "America/New_York");

        schedule.TimeZone.Should().Be("America/New_York");
    }

    [Fact]
    public void Create_WithEmptyCron_ShouldThrow()
    {
        var act = () => ScheduleDefinition.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var s1 = ScheduleDefinition.Create("0 0 * * *");
        var s2 = ScheduleDefinition.Create("0 0 * * *");

        s1.Should().Be(s2);
    }

    [Fact]
    public void Equality_DifferentCron_ShouldNotBeEqual()
    {
        var s1 = ScheduleDefinition.Create("0 0 * * *");
        var s2 = ScheduleDefinition.Create("0 9 * * *");

        s1.Should().NotBe(s2);
    }
}
