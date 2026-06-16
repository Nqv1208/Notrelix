namespace Notrelix.Application.Common.Abstractions;

public interface IEventTypeRegistry
{
    Type? GetEventType(string eventTypeName);
}
