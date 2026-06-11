using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.ShareLinks;

public record ShareLinkCreatedEvent(Guid LinkId, ResourceType ResourceType, Guid ResourceId, Guid CreatedBy) : DomainRecordEvent;
public record ShareLinkDisabledEvent(Guid LinkId, Guid DisabledBy) : DomainRecordEvent;
public record ShareLinkRotatedEvent(Guid LinkId, Guid RotatedBy) : DomainRecordEvent;
public record ShareLinkExpiredEvent(Guid LinkId) : DomainRecordEvent;
