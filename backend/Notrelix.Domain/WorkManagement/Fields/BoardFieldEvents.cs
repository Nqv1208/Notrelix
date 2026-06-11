using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Fields;

public record BoardFieldCreatedEvent(Guid BoardId, Guid FieldId, string Name, FieldType Type, Guid CreatedBy) : DomainRecordEvent;
public record BoardFieldUpdatedEvent(Guid FieldId, Guid BoardId, Guid UpdatedBy) : DomainRecordEvent;
public record BoardFieldDeletedEvent(Guid FieldId, Guid BoardId, Guid DeletedBy) : DomainRecordEvent;
public record BoardFieldRenamedEvent(Guid FieldId, Guid BoardId, string OldName, string NewName, Guid UpdatedBy) : DomainRecordEvent;
public record BoardFieldReorderedEvent(Guid FieldId, Guid BoardId, double NewPosition, Guid UpdatedBy) : DomainRecordEvent;
public record FieldOptionAddedEvent(Guid FieldId, Guid OptionId, string Name, Guid AddedBy) : DomainRecordEvent;
public record FieldOptionUpdatedEvent(Guid FieldId, Guid OptionId, string NewName, Guid UpdatedBy) : DomainRecordEvent;
public record FieldOptionRemovedEvent(Guid FieldId, Guid OptionId, Guid RemovedBy) : DomainRecordEvent;
