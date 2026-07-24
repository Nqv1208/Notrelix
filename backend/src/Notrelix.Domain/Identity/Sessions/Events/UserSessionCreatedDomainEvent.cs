namespace Notrelix.Domain.Identity.Sessions.Events;

[EventName("identity.user-session-created")]
public sealed record UserSessionCreatedDomainEvent : GlobalDomainEvent
{
    public Guid SessionId { get; }
    public Guid UserId { get; }
    public DateTimeOffset CreatedAt { get; }

    public UserSessionCreatedDomainEvent(
        Guid sessionId,
        Guid userId,
        DateTimeOffset createdAt)
        : base(occurredAt: createdAt)
    {
        SessionId = sessionId;
        UserId = userId;
        CreatedAt = createdAt;
    }
}
