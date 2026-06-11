using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Watchers;

public record ResourceWatchedEvent(Guid WatcherId, ResourceRef Target, Guid UserId) : DomainRecordEvent;
public record ResourceUnwatchedEvent(Guid WatcherId) : DomainRecordEvent;
