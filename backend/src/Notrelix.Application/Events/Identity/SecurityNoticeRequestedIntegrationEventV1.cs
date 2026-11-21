namespace Notrelix.Application.Events.Identity;

/// <summary>
/// A change to someone's sign-in security was just committed (a password change or reset, two-step verification on or off, a
/// provider linked or unlinked, a device signed out, an API token created). The same transaction stages this fact, so a change
/// that rolls back sends nothing; a worker consumer turns it into the mail to the account's owner. It carries no secret: the
/// kind, when, which device did it, and the address the notice goes to.
/// </summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.None)]
[EventName("identity.security-notice-requested", Version = 1)]
[EventPiiField("Email",
    Purpose = "Address the notice goes to: the owner of the account whose security changed.",
    ConsumerJustification = "SendSecurityNoticeEmailConsumer queues the notice; the address is the delivery target itself and cannot be derived from stable IDs by a downstream context.")]
[EventPiiField("UserName",
    Purpose = "Greets the owner by name in the notice.",
    ConsumerJustification = "SendSecurityNoticeEmailConsumer renders the greeting; the name is not recoverable from stable IDs by the worker.")]
[EventPiiField("Device",
    Purpose = "The browser and network address the change came from, shown to the owner so they can tell whether it was them.",
    ConsumerJustification = "SendSecurityNoticeEmailConsumer prints it in the notice; the request that holds it has ended by the time the worker runs.")]
public sealed record SecurityNoticeRequestedIntegrationEventV1(
    Guid EventId,
    Guid UserId,
    string Email,
    string UserName,
    string Kind,
    string Device,
    Guid CorrelationId,
    Guid? SubjectId = null,
    Guid? ActorUserId = null,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "identity.security-notice-requested",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: SourceEventId,
    accountId: null,
    workspaceId: null,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt);
