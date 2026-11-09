using Notrelix.Infrastructure.Integrations.Connections;
using Notrelix.Infrastructure.Messaging.Consumers.Integrations;

namespace Notrelix.Infrastructure.Tests.Messaging;

public class WebhookRedeliveryPolicyTests
{
    [Fact]
    public void Budget_IsEightAttemptsWithSevenRedeliveries()
    {
        WebhookRedeliveryPolicy.MaxAttempts.Should().Be(8);
        WebhookRedeliveryPolicy.MaxRedeliveries.Should().Be(7);
        WebhookRedeliveryPolicy.RedeliveryIntervals.Should().HaveCount(WebhookRedeliveryPolicy.MaxRedeliveries,
            "one wait per redelivery, so the intervals and the budget cannot drift apart");
    }

    [Fact]
    public void Intervals_ArePositiveAndNeverShrink()
    {
        var intervals = WebhookRedeliveryPolicy.RedeliveryIntervals;

        intervals.Should().OnlyContain(i => i > TimeSpan.Zero, "a zero wait is a hot loop");
        intervals.Zip(intervals.Skip(1)).Should().OnlyContain(pair => pair.Second >= pair.First);
    }

    [Theory]
    [InlineData(0, WebhookSendOutcome.Transient, true)]
    [InlineData(6, WebhookSendOutcome.Transient, true)]
    [InlineData(7, WebhookSendOutcome.Transient, false)]
    [InlineData(0, WebhookSendOutcome.Permanent, false)]
    [InlineData(0, WebhookSendOutcome.Delivered, false)]
    public void ShouldRedeliver_OnlyTransientWithinTheBudget(int redeliveriesMade, WebhookSendOutcome outcome, bool expected)
    {
        WebhookRedeliveryPolicy.ShouldRedeliver(redeliveriesMade, outcome).Should().Be(expected);
    }

    /// <summary>Replays the consumer's decision: an attempt is made, and a transient failure is redelivered while the budget lasts.</summary>
    private static (int Attempts, bool Delivered) Run(int failuresBeforeSuccess)
    {
        var attempts = 0;
        while (true)
        {
            attempts++;
            var redeliveriesMade = attempts - 1;
            var outcome = attempts > failuresBeforeSuccess ? WebhookSendOutcome.Delivered : WebhookSendOutcome.Transient;
            if (outcome == WebhookSendOutcome.Delivered) return (attempts, true);
            if (!WebhookRedeliveryPolicy.ShouldRedeliver(redeliveriesMade, outcome)) return (attempts, false);
        }
    }

    [Fact]
    public void ReceiverThatFailsSevenTimes_IsDeliveredOnAttemptEight()
    {
        Run(failuresBeforeSuccess: 7).Should().Be((8, true));
    }

    [Fact]
    public void ReceiverThatNeverAnswers_IsGivenUpAfterEightAttempts()
    {
        Run(failuresBeforeSuccess: int.MaxValue).Should().Be((8, false));
    }
}
