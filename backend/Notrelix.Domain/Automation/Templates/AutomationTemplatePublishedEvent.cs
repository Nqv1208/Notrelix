using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Automation.Templates;

public sealed record AutomationTemplatePublishedEvent(
    Guid WorkspaceId,
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
