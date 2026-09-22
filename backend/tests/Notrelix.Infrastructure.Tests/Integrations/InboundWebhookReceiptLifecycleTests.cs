using Notrelix.Infrastructure.Data.Integrations;

namespace Notrelix.Infrastructure.Tests.Integrations;

public sealed class InboundWebhookReceiptLifecycleTests
{
    private static InboundWebhookReceipt CreateReceipt() =>
        InboundWebhookReceipt.Capture(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Google",
            "delivery-1",
            "hash",
            "protected",
            DateTimeOffset.UtcNow);

    [Fact]
    public void MarkProcessed_CannotResurrectFailedReceipt()
    {
        var receipt = CreateReceipt();
        var failedAt = DateTimeOffset.UtcNow;

        receipt.MarkFailed("provider payload unavailable", failedAt, "ProviderPayloadUnavailable");
        receipt.MarkProcessed(failedAt.AddMinutes(1));

        receipt.Status.Should().Be("Failed");
        receipt.TerminalAt.Should().Be(failedAt);
        receipt.FailureCode.Should().Be("ProviderPayloadUnavailable");
    }

    [Fact]
    public void TerminalStates_AreImmutable()
    {
        var receipt = CreateReceipt();
        var terminalAt = DateTimeOffset.UtcNow;

        receipt.MarkBlocked("semantic target is undefined", terminalAt);
        receipt.MarkFailed("later failure", terminalAt.AddMinutes(1));
        receipt.MarkProcessed(terminalAt.AddMinutes(2));

        receipt.Status.Should().Be("Blocked");
        receipt.TerminalAt.Should().Be(terminalAt);
        receipt.FailureCode.Should().Be("SemanticTargetUndefined");
        receipt.FailureDetail.Should().Be("semantic target is undefined");
    }
}
