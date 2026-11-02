using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Email;
using Notrelix.Application.Events.Identity;
using Notrelix.Infrastructure.Messaging.Consumers.Identity.RegistrationNotice;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

public class SendRegistrationNoticeEmailConsumerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RegistrationAttempted_QueuesOneNoticeToTheOwnerWithAPerEventKeyAndNoAccountFact()
    {
        var eventId = Guid.CreateVersion7();
        var integrationEvent = new RegistrationAttemptedOnExistingAccountIntegrationEventV1(
            EventId: eventId,
            Email: "owner@example.com",
            CorrelationId: eventId,
            OccurredAt: Now);

        var emailOutboxWriter = new Mock<IEmailOutboxWriter>();
        QueueRenderedEmailRequest? request = null;
        emailOutboxWriter
            .Setup(w => w.QueueRenderedEmailAsync(It.IsAny<QueueRenderedEmailRequest>(), It.IsAny<CancellationToken>()))
            .Callback<QueueRenderedEmailRequest, CancellationToken>((r, _) => request = r)
            .Returns(Task.CompletedTask);
        var context = new Mock<ConsumeContext<RegistrationAttemptedOnExistingAccountIntegrationEventV1>>();
        context.SetupGet(c => c.Message).Returns(integrationEvent);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await new SendRegistrationNoticeEmailConsumer(
                emailOutboxWriter.Object,
                NullLogger<SendRegistrationNoticeEmailConsumer>.Instance)
            .Consume(context.Object);

        request.Should().NotBeNull();
        request!.DeduplicationKey.Should().Be($"registration-notice:{eventId}");
        request.RecipientEmail.Should().Be("owner@example.com");
        request.RecipientUserId.Should().BeNull("the registering person is not the owner and no user is named");
        request.WorkspaceId.Should().BeNull();
        request.SourceContext.Should().Be("identity");
        request.TemplateKey.Should().Be("registration-attempt-notice");
        request.BodyHtml.Should().Contain("account already exists");
        request.BodyText.Should().Contain("account already exists");
        emailOutboxWriter.Verify(
            w => w.QueueRenderedEmailAsync(It.IsAny<QueueRenderedEmailRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
