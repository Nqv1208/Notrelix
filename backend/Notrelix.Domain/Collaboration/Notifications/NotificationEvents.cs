using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Notifications;

public record NotificationCreatedEvent(Guid NotificationId, Guid UserId) : DomainRecordEvent;
public record NotificationReadEvent(Guid NotificationId) : DomainRecordEvent;
public record NotificationArchivedEvent(Guid NotificationId) : DomainRecordEvent;
