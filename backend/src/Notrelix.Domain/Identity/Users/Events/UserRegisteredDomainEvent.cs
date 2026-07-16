using Notrelix.Domain.Common.Constants;

namespace Notrelix.Domain.Identity.Users.Events;

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
        : base(occurredAt: registeredAt, actorUserId: userId, subjectId: userId)
    {
        UserId = userId;
        Email = email;
        DisplayName = displayName;
        RegisteredAt = registeredAt;

        SourceContext = SourceContexts.Identity;
        AggregateType = AggregateTypes.User;
        AggregateId = userId;
        SubjectType = SubjectTypes.User;
    }
}
