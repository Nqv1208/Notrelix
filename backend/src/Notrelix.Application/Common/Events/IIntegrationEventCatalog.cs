namespace Notrelix.Application.Common.Events;

public interface IIntegrationEventCatalog
{
    Type Resolve(string messageName);
    bool TryResolve(string messageName, out Type type);
}
