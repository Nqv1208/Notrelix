using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Email;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Infrastructure.Messaging.Consumers.Identity.SecurityNotice;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

public class SendSecurityNoticeEmailConsumerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 3, 0, 0, TimeSpan.Zero);
    private readonly Mock<IEmailOutboxWriter> _outbox = new();
    private readonly Mock<ISecurityPageLink> _link = new();
    private readonly List<QueueRenderedEmailRequest> _queued = new();

    public SendSecurityNoticeEmailConsumerTests()
    {
        _link.Setup(l => l.Build()).Returns("https://app.example.test/security");
        _outbox.Setup(o => o.QueueRenderedEmailAsync(It.IsAny<QueueRenderedEmailRequest>(), It.IsAny<CancellationToken>()))
            .Callback<QueueRenderedEmailRequest, CancellationToken>((r, _) => _queued.Add(r))
            .Returns(Task.CompletedTask);
    }

    private static SecurityNoticeRequestedIntegrationEventV1 Event(string kind = "PasswordReset", string device = "Safari · 203.0.113.9", Guid? id = null)
    {
        var eventId = id ?? Guid.CreateVersion7();
        return new SecurityNoticeRequestedIntegrationEventV1(
            EventId: eventId, UserId: Guid.CreateVersion7(), Email: "owner@example.com", UserName: "Hà", Kind: kind,
            Device: device, CorrelationId: eventId, OccurredAt: Now);
    }

    private Task Consume(SecurityNoticeRequestedIntegrationEventV1 message)
    {
        var context = new Mock<ConsumeContext<SecurityNoticeRequestedIntegrationEventV1>>();
        context.SetupGet(c => c.Message).Returns(message);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return new SendSecurityNoticeEmailConsumer(_outbox.Object, _link.Object, NullLogger<SendSecurityNoticeEmailConsumer>.Instance)
            .Consume(context.Object);
    }

    [Theory]
    [InlineData("PasswordChanged")]
    [InlineData("PasswordReset")]
    [InlineData("MfaEnabled")]
    [InlineData("MfaDisabled")]
    [InlineData("ProviderLinked")]
    [InlineData("ProviderUnlinked")]
    [InlineData("SessionRevoked")]
    [InlineData("OtherSessionsRevoked")]
    [InlineData("ApiTokenIssued")]
    public async Task EveryKind_QueuesOneMailToTheOwner_WithTheTimeTheDeviceAndALink(string kind)
    {
        var message = Event(kind);

        await Consume(message);

        var mail = _queued.Should().ContainSingle().Subject;
        mail.RecipientEmail.Should().Be("owner@example.com");
        mail.RecipientUserId.Should().Be(message.UserId);
        mail.WorkspaceId.Should().BeNull();
        mail.Subject.Should().EndWith("— Notrelix").And.NotBeNullOrWhiteSpace();
        mail.BodyHtml.Should().Contain("Safari").And.Contain("203.0.113.9").And.Contain("2026-11-19 03:00 UTC").And.Contain("https://app.example.test/security");
        mail.SourceEventId.Should().Be(message.EventId);
    }

    [Fact]
    public async Task ARedelivery_QueuesTheSameKey_SoTheOutboxKeepsOneMail_AndAnotherEventGetsAnotherKey()
    {
        var message = Event();

        await Consume(message);
        await Consume(message);
        await Consume(Event());

        _queued[0].DeduplicationKey.Should().Be($"identity.security-notice:{message.EventId}");
        _queued[1].DeduplicationKey.Should().Be(_queued[0].DeduplicationKey);
        _queued[2].DeduplicationKey.Should().NotBe(_queued[0].DeduplicationKey);
    }

    [Fact]
    public async Task TheDeviceTextIsEncoded_SoAnAgentStringCannotInjectMarkup()
    {
        await Consume(Event(device: "<script>alert(1)</script>"));

        _queued.Single().BodyHtml.Should().NotContain("<script>").And.Contain("&lt;script&gt;");
    }

    [Fact]
    public async Task AKindANewerProducerAdded_IsSkippedWithoutFailingTheDelivery()
    {
        var act = () => Consume(Event(kind: "SomethingNew"));

        await act.Should().NotThrowAsync();
        _queued.Should().BeEmpty();
    }
}
