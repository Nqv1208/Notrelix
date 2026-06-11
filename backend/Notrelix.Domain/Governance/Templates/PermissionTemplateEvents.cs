using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Templates;

public enum PermissionTemplateStatus
{
    Active,
    Archived
}

public record PermissionTemplateCreatedEvent(Guid TemplateId, string Name, Guid CreatedBy) : DomainRecordEvent;
public record PermissionTemplateAppliedEvent(Guid TemplateId, Guid TargetResourceId, Guid AppliedBy) : DomainRecordEvent;
