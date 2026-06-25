namespace Notrelix.Domain.Integrations.Calendar;

public class CalendarEvent : Entity
{
    public Guid IntegrationId { get; private set; }
    public string ExternalEventId { get; private set; } = null!;
    public ResourceRef Target { get; private set; } = null!;
    public SyncHash SyncHash { get; private set; } = null!;

    private CalendarEvent() : base() { }

    public static CalendarEvent Create(Guid integrationId, string externalEventId, ResourceRef target, SyncHash syncHash)
    {
        Guard.NotEmpty(integrationId);
        Guard.NotNullOrWhiteSpace(externalEventId);
        Guard.NotNull(target);
        Guard.NotNull(syncHash);

        return new CalendarEvent
        {
            IntegrationId = integrationId,
            ExternalEventId = externalEventId,
            Target = target,
            SyncHash = syncHash
        };
    }

    public void UpdateSyncHash(SyncHash syncHash)
    {
        SyncHash = syncHash ?? throw new ArgumentNullException(nameof(syncHash));
    }
}
