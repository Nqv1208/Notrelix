using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Relations;

public record RelationFieldConfiguredEvent(Guid FieldId, Guid SourceBoardId, Guid TargetBoardId) : DomainRecordEvent;
