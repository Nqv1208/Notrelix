using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Integrations.Webhooks.Events;
using Xunit;

namespace Notrelix.Domain.Tests.Integrations;

public class InboundWebhookEventTests
{
    [Fact]
    public void Record_ShouldCreateEvent()
    {
        var payload = JsonValue.Create("{\"event\":\"test\"}");

        var evt = InboundWebhookEvent.Record("github", "push", payload, DateTimeOffset.UtcNow);

        evt.Provider.Should().Be("github");
        evt.EventType.Should().Be("push");
        evt.Payload.Should().Be(payload);
    }

    [Fact]
    public void Record_WithWorkspaceAndExternalId_ShouldSetThem()
    {
        var evt = InboundWebhookEvent.Record("stripe", "invoice.paid", JsonValue.EmptyObject(), DateTimeOffset.UtcNow, Guid.NewGuid(), "evt_123");

        evt.WorkspaceId.Should().NotBeNull();
        evt.ExternalEventId.Should().Be("evt_123");
    }
}
