using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Checklists;

public record ChecklistCreatedEvent(Guid ItemId, Guid ChecklistId, string Title) : DomainRecordEvent;
public record ChecklistItemAddedEvent(Guid ChecklistId, Guid ItemId, string Title) : DomainRecordEvent;
public record ChecklistItemToggledEvent(Guid ChecklistId, Guid ItemId, bool IsDone) : DomainRecordEvent;
public record ChecklistItemRemovedEvent(Guid ChecklistId, Guid ItemId) : DomainRecordEvent;
