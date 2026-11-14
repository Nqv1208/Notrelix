using Notrelix.Infrastructure.Integrations.Connections;

namespace Notrelix.Infrastructure.Tests.Integrations.Connections;

public class WebhookRetryAfterTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SecondsForm_IsHonored()
    {
        WebhookRetryAfter.Capped(TimeSpan.FromSeconds(90), null, Now).Should().Be(TimeSpan.FromSeconds(90));
    }

    [Fact]
    public void DateForm_IsTheWaitUntilThatTime()
    {
        WebhookRetryAfter.Capped(null, Now.AddMinutes(3), Now).Should().Be(TimeSpan.FromMinutes(3));
    }

    [Fact]
    public void DateInThePast_MeansNoWait()
    {
        WebhookRetryAfter.Capped(null, Now.AddMinutes(-3), Now).Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void LongWait_IsCappedAtOneHour()
    {
        WebhookRetryAfter.Capped(TimeSpan.FromDays(2), null, Now).Should().Be(TimeSpan.FromHours(1));
        WebhookRetryAfter.Capped(null, Now.AddDays(2), Now).Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void NoHeader_IsNull()
    {
        WebhookRetryAfter.Capped(null, null, Now).Should().BeNull();
    }

    [Fact]
    public void Cap_IsOneHour()
    {
        WebhookRetryAfter.Cap.Should().Be(TimeSpan.FromHours(1));
    }
}
