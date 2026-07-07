namespace Notrelix.Application.Common.Events;

public sealed class UnknownIntegrationEventTypeException : InvalidOperationException
{
    public string MessageName { get; }

    public UnknownIntegrationEventTypeException(string messageName)
        : base($"Integration event type '{messageName}' is not registered in the catalog. This is a permanent failure.")
    {
        MessageName = messageName;
    }
}
