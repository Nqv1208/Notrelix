using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Views;

public record BoardViewCreatedEvent(Guid BoardId, Guid ViewId, string Name, ViewType Type, Guid CreatedBy) : DomainRecordEvent;
public record BoardViewRenamedEvent(Guid ViewId, string OldName, string NewName, Guid UpdatedBy) : DomainRecordEvent;
public record BoardViewConfigUpdatedEvent(Guid ViewId, Guid BoardId, Guid UpdatedBy) : DomainRecordEvent;
public record BoardViewDeletedEvent(Guid ViewId, Guid BoardId, Guid DeletedBy) : DomainRecordEvent;
