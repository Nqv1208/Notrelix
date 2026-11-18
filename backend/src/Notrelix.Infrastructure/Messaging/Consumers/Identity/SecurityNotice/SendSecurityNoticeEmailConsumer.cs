using System.Globalization;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Identity.Security.Abstractions;

namespace Notrelix.Infrastructure.Messaging.Consumers.Identity.SecurityNotice;

[OrderingNotApplicable("Queues one notice per security change through a per-event deduplication key; the notices are independent and nothing supersedes an earlier one.")]
public sealed class SendSecurityNoticeEmailConsumer : IConsumer<SecurityNoticeRequestedIntegrationEventV1>
{
    private readonly IEmailOutboxWriter _emailOutboxWriter;
    private readonly ISecurityPageLink _link;
    private readonly ILogger<SendSecurityNoticeEmailConsumer> _logger;

    public SendSecurityNoticeEmailConsumer(
        IEmailOutboxWriter emailOutboxWriter,
        ISecurityPageLink link,
        ILogger<SendSecurityNoticeEmailConsumer> logger)
    {
        _emailOutboxWriter = emailOutboxWriter;
        _link = link;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SecurityNoticeRequestedIntegrationEventV1> context)
    {
        var msg = context.Message;
        if (!Enum.TryParse<SecurityNoticeKind>(msg.Kind, out var kind) || !Enum.IsDefined(kind))
        {
            // A kind a newer producer added: nothing sensible to say, and a retry would not change that.
            _logger.LogWarning("Security notice skipped: unknown kind {Kind}. EventId={EventId}", msg.Kind, msg.EventId);
            return;
        }

        var (headline, detail) = Describe(kind);
        var html = EmailTemplateService.SecurityNotice(
            msg.UserName,
            headline,
            detail,
            msg.OccurredAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC",
            msg.Device,
            _link.Build());

        // The per-event key makes a redelivery queue one mail.
        await _emailOutboxWriter.QueueRenderedEmailAsync(
            new QueueRenderedEmailRequest(
                DeduplicationKey: $"identity.security-notice:{msg.EventId}",
                RecipientEmail: msg.Email,
                RecipientName: msg.UserName,
                Subject: $"{headline} — Notrelix",
                BodyHtml: html,
                BodyText: null,
                WorkspaceId: null,
                RecipientUserId: msg.UserId,
                SourceContext: "identity",
                TemplateKey: "identity.security-notice",
                SourceEventId: msg.EventId,
                SourceMessageId: context.MessageId),
            context.CancellationToken);

        // The address and the device are delivery content, not log fields.
        _logger.LogInformation("Security notice queued. EventId={EventId} Kind={Kind}", msg.EventId, kind);
    }

    private static (string Headline, string Detail) Describe(SecurityNoticeKind kind) => kind switch
    {
        SecurityNoticeKind.PasswordChanged => (
            "Password changed",
            "The password of your account was changed. All active sessions were signed out; sign in again with the new password."),
        SecurityNoticeKind.PasswordReset => (
            "Password reset",
            "The password of your account was reset with the emailed code. All active sessions were signed out."),
        SecurityNoticeKind.MfaEnabled => (
            "Two-step verification turned on",
            "Two-step verification was turned on for your account."),
        SecurityNoticeKind.MfaDisabled => (
            "Two-step verification turned off",
            "Two-step verification was turned off for your account."),
        SecurityNoticeKind.ProviderLinked => (
            "Sign-in provider linked",
            "A sign-in provider was linked to your account."),
        SecurityNoticeKind.ProviderUnlinked => (
            "Sign-in provider unlinked",
            "A sign-in provider was unlinked from your account."),
        SecurityNoticeKind.SessionRevoked => (
            "A device was signed out",
            "One device was signed out of your account."),
        SecurityNoticeKind.OtherSessionsRevoked => (
            "Other devices were signed out",
            "Every device except the one that did it was signed out of your account."),
        SecurityNoticeKind.ApiTokenIssued => (
            "A new API token was created",
            "A new API token was created under your account. It can call Notrelix on your behalf until it expires or is revoked."),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
