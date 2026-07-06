namespace Notrelix.Application.Common.Events;

public interface IEventTypeRegistry
{
    Type? GetEventType(string messageName);
    string GetMessageName(Type type);
    string GetMessageName<T>();
}
