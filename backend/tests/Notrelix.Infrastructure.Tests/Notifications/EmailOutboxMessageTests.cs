using Notrelix.Application.Common.Email;
using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Tests.Notifications;

public sealed class EmailOutboxMessageTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RenderedFactory_ShouldKeepRenderedBodyAndNoTemplatePayload()
    {
        var message = EmailOutboxMessage.CreateRendered(
            new QueueRenderedEmailRequest(
                "welcome:user",
                "person@example.com",
                "Person",
                "Welcome",
                "<p>Welcome</p>",
                null,
                null,
                null,
                "identity",
                "welcome-email"),
            Now);

        message.ContentMode.Should().Be(EmailContentMode.Rendered);
        message.Subject.Should().Be("Welcome");
        message.BodyHtml.Should().NotBeNull();
        message.TemplateDataJson.Should().BeNull();
    }

    [Fact]
    public void TemplatedFactory_ShouldNotPersistRenderedSubjectOrBody()
    {
        var message = EmailOutboxMessage.CreateTemplated(
            new QueueTemplatedEmailRequest<WorkspaceInvitationEmailPayload>(
                "workspace-invitation:id:generation:1",
                "person@example.com",
                null,
                null,
                null,
                "workspaces",
                "workspace-invitation",
                1,
                new WorkspaceInvitationEmailPayload(
                    Guid.NewGuid(),
                    1,
                    new ProtectedSecretEnvelope("protected"),
                    Now.AddHours(1)),
                Now.AddHours(1)),
            Now);

        message.ContentMode.Should().Be(EmailContentMode.Templated);
        message.Subject.Should().BeNull();
        message.BodyHtml.Should().BeNull();
        message.TemplateDataJson.Should().NotBeNull();
    }

    [Fact]
    public void SentMessage_ShouldClearProtectedTemplatePayload()
    {
        var message = EmailOutboxMessage.CreateTemplated(
            new QueueTemplatedEmailRequest<EmailVerificationEmailPayload>(
                "email-verification:id",
                "person@example.com",
                null,
                null,
                Guid.NewGuid(),
                "identity",
                "email-verification",
                1,
                new EmailVerificationEmailPayload(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new ProtectedSecretEnvelope("protected"),
                    Now.AddHours(1)),
                Now.AddHours(1)),
            Now);

        message.MarkProcessing("dispatcher", "lease", Now, 120);
        message.MarkSent("noop", "message-id", Now.AddMinutes(1), "lease");

        message.Status.Should().Be("Sent");
        message.TemplateDataJson.Should().BeNull();
        message.SensitivePayloadClearedAt.Should().NotBeNull();
    }

    [Fact]
    public void CancelledMessage_ShouldSetContentModePurgedAndClearPayload()
    {
        var message = EmailOutboxMessage.CreateTemplated(
            new QueueTemplatedEmailRequest<EmailVerificationEmailPayload>(
                "email-verification:id",
                "person@example.com",
                null,
                null,
                Guid.NewGuid(),
                "identity",
                "email-verification",
                1,
                new EmailVerificationEmailPayload(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new ProtectedSecretEnvelope("protected"),
                    Now.AddHours(1)),
                Now.AddHours(1)),
            Now);

        message.MarkCancelled("test-reason", Now);

        message.Status.Should().Be("Cancelled");
        message.ContentMode.Should().Be(EmailContentMode.Purged);
        message.TemplateDataJson.Should().BeNull();
        message.SensitivePayloadClearedAt.Should().NotBeNull();
    }

    [Fact]
    public void DeadLetterMessage_ShouldSetContentModePurgedAndClearPayload()
    {
        var message = EmailOutboxMessage.CreateTemplated(
            new QueueTemplatedEmailRequest<EmailVerificationEmailPayload>(
                "email-verification:id",
                "person@example.com",
                null,
                null,
                Guid.NewGuid(),
                "identity",
                "email-verification",
                1,
                new EmailVerificationEmailPayload(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new ProtectedSecretEnvelope("protected"),
                    Now.AddHours(1)),
                Now.AddHours(1)),
            Now);

        message.MarkDeadLetter(Now);

        message.Status.Should().Be("Failed",
            "dead-letter is the exhausted-retry 'Failed' terminal state, not a separate status");
        message.RetryCount.Should().Be(message.MaxRetries);
        message.ContentMode.Should().Be(EmailContentMode.Purged);
        message.TemplateDataJson.Should().BeNull();
        message.SensitivePayloadClearedAt.Should().NotBeNull();
    }

    [Fact]
    public void SentMessage_CannotBeSettledAgain_SoTheMailIsNotSentTwice()
    {
        var message = RenderedMessage();
        message.MarkProcessing("dispatcher", "lease-1", Now, 120);
        message.MarkSent("smtp", "message-1", Now.AddMinutes(1), "lease-1");

        var again = () => message.MarkSent("smtp", "message-2", Now.AddMinutes(2), "lease-1");

        again.Should().Throw<InvalidOperationException>();
        message.ProviderMessageId.Should().Be("message-1");
    }

    [Fact]
    public void FailedAttempt_IsRetried_AndTheRecoveredAttemptSendsOnce()
    {
        var message = RenderedMessage();
        message.MarkProcessing("dispatcher", "lease-1", Now, 120);
        message.MarkFailed("smtp_unavailable", "connection refused", Now.AddSeconds(5), "lease-1");
        message.ScheduleRetry(Now.AddMinutes(1), Now.AddSeconds(5));

        message.Status.Should().Be("Failed");
        message.RetryCount.Should().Be(1);

        message.MarkProcessing("dispatcher", "lease-2", Now.AddMinutes(2), 120);
        message.MarkSent("smtp", "message-1", Now.AddMinutes(2), "lease-2");

        message.Status.Should().Be("Sent");
        var second = () => message.MarkSent("smtp", "message-2", Now.AddMinutes(3), "lease-2");
        second.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void StaleLease_CannotSettleAMessageTakenOverByAnotherWorker()
    {
        var message = RenderedMessage();
        message.MarkProcessing("dispatcher", "lease-1", Now, 120);
        message.MarkProcessing("dispatcher", "lease-2", Now.AddMinutes(3), 120);

        var stale = () => message.MarkSent("smtp", "message-1", Now.AddMinutes(3), "lease-1");

        stale.Should().Throw<InvalidOperationException>();
        message.Status.Should().Be("Sending");
    }

    private static EmailOutboxMessage RenderedMessage() =>
        EmailOutboxMessage.CreateRendered(
            new QueueRenderedEmailRequest(
                "welcome:user",
                "person@example.com",
                "Person",
                "Welcome",
                "<p>Welcome</p>",
                null,
                null,
                null,
                "identity",
                "welcome-email"),
            Now);
}
