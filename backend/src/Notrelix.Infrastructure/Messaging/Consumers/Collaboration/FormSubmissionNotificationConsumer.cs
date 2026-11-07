using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Public.FormNotifications;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Infrastructure.Data.Abstractions;
using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

/// <summary>
/// "New submission on a form I manage" (D78.2): each person who manages the form and can still see its Board is
/// told once in-app, unless they answered it themselves or turned the matching preference off. The notification
/// names the form and links to its Board; it never carries an answer, because a response can hold respondent data.
/// </summary>
[OrderingNotApplicable("Each submission notifies each manager once, keyed by the submission and person; deliveries commute.")]
public sealed class FormSubmissionNotificationConsumer : IConsumer<FormSubmissionCreatedIntegrationEvent>
{
    public const string BoardKind = "work-management.board";

    private readonly INotificationDbContext _notifications;
    private readonly IFormNotificationFacts _forms;
    private readonly IResourceVisibility _visibility;

    public FormSubmissionNotificationConsumer(
        INotificationDbContext notifications,
        IFormNotificationFacts forms,
        IResourceVisibility visibility)
    {
        _notifications = notifications;
        _forms = forms;
        _visibility = visibility;
    }

    public async Task Consume(ConsumeContext<FormSubmissionCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        if (msg.AccountIdValue == Guid.Empty || msg.WorkspaceIdValue == Guid.Empty) return;

        // A form or Board that is gone tells nobody (fail closed).
        var facts = await _forms.FindAsync(msg.WorkspaceIdValue, msg.FormId, ct);
        if (facts is null || facts.BoardId != msg.BoardId) return;

        var candidates = facts.ManagerUserIds
            .Where(id => id != Guid.Empty && id != msg.SubmitterUserId)
            .Distinct()
            .ToList();
        // shortcut: one visibility read per person (no batched read exists), fine for the few managers of a form.
        var recipients = new List<Guid>();
        foreach (var userId in candidates)
        {
            if ((await _visibility.VisibleBoardIdsAsync(msg.WorkspaceIdValue, userId, ct)).Contains(facts.BoardId))
                recipients.Add(userId);
        }
        if (recipients.Count == 0) return;

        var prefix = $"{NotificationTypes.FormSubmissionCreatedProducedType}:{msg.SubmissionId}:";
        var keys = recipients.Select(id => prefix + id).ToList();
        var existing = (await _notifications.NotificationItems
                .AsNoTracking()
                .Where(n => n.DeduplicationKey != null && keys.Contains(n.DeduplicationKey))
                .Select(n => n.DeduplicationKey!)
                .ToListAsync(ct))
            .ToHashSet(StringComparer.Ordinal);

        var preferenceType = NotificationTypes.FromProducedType(NotificationTypes.FormSubmissionCreatedProducedType)!;
        var sets = await _notifications.NotificationPreferenceSets
            .AsNoTracking()
            .Include(s => s.Entries)
            .Where(s => s.AccountId == msg.AccountIdValue && s.WorkspaceId == msg.WorkspaceIdValue && recipients.Contains(s.UserId))
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var added = false;
        foreach (var userId in recipients)
        {
            var key = prefix + userId;
            if (existing.Contains(key)) continue;

            var set = sets.FirstOrDefault(s => s.UserId == userId);
            var enabled = set?.IsEnabled(preferenceType, NotificationChannels.InApp)
                ?? NotificationPreferenceDefaults.IsEnabled(preferenceType, NotificationChannels.InApp);
            if (!enabled) continue;

            var item = NotificationItemRecord.Create(
                accountId: msg.AccountIdValue,
                workspaceId: msg.WorkspaceIdValue,
                sourceContext: "collaboration",
                notificationType: NotificationTypes.FormSubmissionCreatedProducedType,
                severity: NotificationSeverity.Info,
                title: "New form response",
                createdAt: now,
                actorUserId: msg.SubmitterUserId,
                sourceEventId: msg.EventId,
                subjectType: "FormSubmission",
                subjectId: msg.SubmissionId,
                resourceType: BoardKind,
                resourceId: facts.BoardId,
                body: ItemFollowerNotifier.Excerpt(facts.FormName),
                deduplicationKey: key);
            _notifications.NotificationItems.Add(item);
            _notifications.NotificationRecipients.Add(NotificationRecipientRecord.Create(
                accountId: msg.AccountIdValue,
                notificationId: item.Id,
                workspaceId: msg.WorkspaceIdValue,
                recipientUserId: userId,
                createdAt: now));
            added = true;
        }

        if (added) await _notifications.SaveChangesAsync(ct);
    }
}
