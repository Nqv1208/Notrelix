namespace Notrelix.Application.Common.Events;

public interface IDomainEventDispatchPolicy
{
    DomainEventDispatchMode GetMode(Type eventType);
    IReadOnlyCollection<Type> GetInlineTypes();
}
