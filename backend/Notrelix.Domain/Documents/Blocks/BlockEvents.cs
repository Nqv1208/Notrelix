using Notrelix.Domain.Common;

namespace Notrelix.Domain.Documents.Blocks;

public record BlockCreatedEvent(Guid PageId, Guid BlockId, BlockType Type, Guid CreatedBy) : DomainRecordEvent;
public record BlockUpdatedEvent(Guid BlockId, Guid PageId, Guid UpdatedBy) : DomainRecordEvent;
public record BlockMovedEvent(Guid BlockId, Guid PageId, Guid? OldParentId, Guid? NewParentId, double NewPosition, Guid UpdatedBy) : DomainRecordEvent;
public record BlockDeletedEvent(Guid BlockId, Guid PageId, Guid DeletedBy) : DomainRecordEvent;
