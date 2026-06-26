using FluentAssertions;
using Notrelix.Domain.Billing.Usage;

namespace Notrelix.Domain.Tests.Billing;

public class UsagePeriodTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var period = UsagePeriod.Create(start, end);

        period.Start.Should().Be(start);
        period.End.Should().Be(end);
    }

    [Fact]
    public void Create_WhenStartAfterEnd_ShouldThrow()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(-1);

        var act = () => UsagePeriod.Create(start, end);
        act.Should().Throw<DomainException>().WithMessage("*start must be before end*");
    }

    [Fact]
    public void Create_WhenStartEqualsEnd_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var act = () => UsagePeriod.Create(now, now);
        act.Should().Throw<DomainException>().WithMessage("*start must be before end*");
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var now = DateTimeOffset.UtcNow;
        var p1 = UsagePeriod.Create(now, now.AddDays(30));
        var p2 = UsagePeriod.Create(now, now.AddDays(30));

        p1.Should().Be(p2);
    }

    [Fact]
    public void Equality_DifferentEnd_ShouldNotBeEqual()
    {
        var now = DateTimeOffset.UtcNow;
        var p1 = UsagePeriod.Create(now, now.AddDays(30));
        var p2 = UsagePeriod.Create(now, now.AddDays(60));

        p1.Should().NotBe(p2);
    }
}
