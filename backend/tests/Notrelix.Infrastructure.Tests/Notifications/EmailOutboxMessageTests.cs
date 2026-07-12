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
}
