using System.Net;
using Notrelix.Application.Features.Automation.Ports;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

namespace Notrelix.Infrastructure.CrossContext.Automation.Messaging;

/// <summary>
/// Runtime adapter of the Automation message port: the in-app notification goes through the
/// notification pipeline every other producer uses, the email through the email outbox (so the
/// message commits with the execution and is delivered, or logged in development, afterwards).
/// </summary>
public sealed class AutomationMessageAdapter(
    ItemFollowerNotifier notifier,
    IEmailOutboxWriter emailOutbox,
    IIdentityUserFacts identityFacts,
    IDateTimeProvider clock) : IAutomationMessagePort
{
    public Task NotifyMemberAsync(
        Guid accountId, Guid workspaceId, Guid recipientUserId, Guid? itemId, Guid? actorUserId,
        Guid operationId, string title, string? body, CancellationToken cancellationToken) =>
        notifier.NotifyAsync(
            accountId, workspaceId, itemId ?? Guid.Empty, actorUserId, operationId,
            NotificationTypes.AutomationProducedType, title, body, [recipientUserId], clock.UtcNow, cancellationToken);

    public async Task<bool> SendEmailAsync(
        Guid accountId, Guid workspaceId, Guid recipientUserId, Guid operationId,
        string subject, string? body, CancellationToken cancellationToken)
    {
        var emails = await identityFacts.FindEmailsAsync(workspaceId, [recipientUserId], cancellationToken);
        if (!emails.TryGetValue(recipientUserId, out var email))
        {
            return false;
        }

        var text = body ?? string.Empty;
        await emailOutbox.QueueRenderedEmailAsync(
            new QueueRenderedEmailRequest(
                DeduplicationKey: $"automation-email:{operationId:N}:{recipientUserId:N}",
                RecipientEmail: email,
                RecipientName: null,
                Subject: subject,
                BodyHtml: $"<p>{WebUtility.HtmlEncode(text)}</p>",
                BodyText: text,
                WorkspaceId: workspaceId,
                RecipientUserId: recipientUserId,
                SourceContext: "automation",
                TemplateKey: "automation.send-email"),
            cancellationToken);
        return true;
    }
}
