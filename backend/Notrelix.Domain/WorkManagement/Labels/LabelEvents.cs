using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Labels;

public record LabelCreatedEvent(Guid BoardId, Guid LabelId, string Name) : DomainRecordEvent;
public record LabelUpdatedEvent(Guid LabelId, Guid UpdatedBy) : DomainRecordEvent;
public record LabelSoftDeletedEvent(Guid LabelId, Guid DeletedBy) : DomainRecordEvent;
