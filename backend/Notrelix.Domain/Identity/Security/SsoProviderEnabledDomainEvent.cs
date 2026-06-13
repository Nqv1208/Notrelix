using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security;

public record SsoProviderEnabledDomainEvent : DomainEvent
{
    public Guid ProviderId { get; }
    public string Name { get; }

    public SsoProviderEnabledDomainEvent(
        Guid workspaceId,
        Guid providerId,
        string name,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        ProviderId = providerId;
        Name = name;
    }
}
