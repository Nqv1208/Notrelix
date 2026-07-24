namespace Notrelix.Domain.Identity.Users.Events;

[EventName("identity.user-registered")]
public sealed record UserRegisteredDomainEvent : GlobalDomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string DisplayName { get; }
    public DateTimeOffset RegisteredAt { get; }

    public UserRegisteredDomainEvent(
        Guid userId,
        string email,
        string displayName,
        DateTimeOffset registeredAt)
        : base(occurredAt: registeredAt)
    {
        UserId = userId;
        Email = email;
        DisplayName = displayName;
        RegisteredAt = registeredAt;
    }
}
