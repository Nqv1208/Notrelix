using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Automation.Templates;

public sealed record AutomationTemplateCreatedEvent(
    Guid WorkspaceId,
    Guid TemplateId,
    string Name,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
