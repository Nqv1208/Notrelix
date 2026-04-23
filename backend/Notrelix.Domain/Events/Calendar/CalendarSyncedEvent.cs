using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Calendar;

public class CalendarSyncedEvent : BaseEvent
{
    public Guid IntegrationId { get; }
    public int SyncedCount { get; }

    public CalendarSyncedEvent(Guid integrationId, int syncedCount)
    {
        IntegrationId = integrationId;
        SyncedCount = syncedCount;
    }
}
