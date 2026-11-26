using Notrelix.Application.Features.Collaboration.Notifications.Realtime;
using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Data.Interceptors;

/// <summary>
/// Announces an Inbox change to its recipient (UC-COL-E02). Every producer of an Inbox entry (a mention, a followed
/// item, a form response, a chat handoff, a board transfer) adds a <see cref="NotificationRecipientRecord"/>; before the
/// save, this enrolls one <see cref="InboxRealtime.Changed"/> fact per recipient and Workspace in the scope's collector.
/// Read, done and snooze are written with ExecuteUpdate, which no interceptor sees. It is registered ahead of <see cref="DomainEventInterceptor"/>,
/// which writes the collector to the outbox in the same transaction, so a frame goes out only for a committed change.
/// </summary>
public sealed class InboxRealtimeInterceptor : SaveChangesInterceptor
{
    private readonly IIntegrationEventCollector _events;
    private readonly IDateTimeProvider _time;

    public InboxRealtimeInterceptor(IIntegrationEventCollector events, IDateTimeProvider time)
    {
        _events = events;
        _time = time;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Enroll(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Enroll(eventData.Context);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    private void Enroll(DbContext? context)
    {
        if (context is null) return;

        var changed = context.ChangeTracker.Entries<NotificationRecipientRecord>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Select(e => (e.Entity.AccountId, e.Entity.WorkspaceId, e.Entity.RecipientUserId))
            .Distinct()
            .ToList();
        if (changed.Count == 0) return;

        var now = _time.UtcNow;
        foreach (var (accountId, workspaceId, userId) in changed)
            _events.Add(InboxRealtime.Changed(accountId, workspaceId, userId, correlationId: null, causationId: null, now));
    }
}
