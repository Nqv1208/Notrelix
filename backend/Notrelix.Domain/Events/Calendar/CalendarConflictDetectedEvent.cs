using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Events.Calendar;

public class CalendarConflictDetectedEvent : BaseEvent
{
    public Guid IntegrationId { get; }
    public ResourceType ResourceType { get; }
    public Guid ResourceId { get; }
    public string Message { get; }

    public CalendarConflictDetectedEvent(Guid integrationId, ResourceType resourceType, Guid resourceId, string message)
    {
        IntegrationId = integrationId;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Message = message;
    }
}
