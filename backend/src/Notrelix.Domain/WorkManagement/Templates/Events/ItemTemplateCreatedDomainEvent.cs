namespace Notrelix.Domain.WorkManagement.Templates.Events;

public sealed record ItemTemplateCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TemplateId,
    string Name,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
