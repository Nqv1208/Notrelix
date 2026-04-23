using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities.Calendar;

/// <summary>
/// Mapping giữa card/page ↔ external calendar event
/// </summary>
public class CalendarEvent : BaseEntity
{
    public Guid IntegrationId { get; private set; }
    public string ExternalEventId { get; private set; } = null!;
    public ResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public string? SyncHash { get; private set; }
    public DateTime SyncedAt { get; private set; }

    // Navigation
    public CalendarIntegration Integration { get; private set; } = null!;

    private CalendarEvent() : base() { }

    public static CalendarEvent Create(
        Guid integrationId,
        string externalEventId,
        ResourceType resourceType,
        Guid resourceId,
        string? syncHash = null)
    {
        return new CalendarEvent
        {
            IntegrationId = integrationId,
            ExternalEventId = externalEventId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            SyncHash = syncHash,
            SyncedAt = DateTime.UtcNow
        };
    }

    public void UpdateSyncHash(string? hash)
    {
        SyncHash = hash;
        SyncedAt = DateTime.UtcNow;
    }
}
