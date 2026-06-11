using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Policies;

public record WorkspacePolicyUpdatedEvent(Guid WorkspaceId, Guid UpdatedBy) : DomainRecordEvent;
