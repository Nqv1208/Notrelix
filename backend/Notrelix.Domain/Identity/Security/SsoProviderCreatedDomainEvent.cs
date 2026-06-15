using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security;

public record SsoProviderCreatedDomainEvent : DomainEvent
{
    public Guid ProviderId { get; }
    public string Name { get; }

    public SsoProviderCreatedDomainEvent(
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
