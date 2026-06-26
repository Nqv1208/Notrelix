namespace Notrelix.Application.Common.Events;

public enum DomainEventDispatchMode
{
    Inline,
    Outbox,
    Ignore,
}
