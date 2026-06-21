namespace Notrelix.Application.Common.Abstractions;

public interface IEventTypeRegistry
{
    Type? GetEventType(string messageName);
    string GetMessageName(Type type);
    string GetMessageName<T>();
}
