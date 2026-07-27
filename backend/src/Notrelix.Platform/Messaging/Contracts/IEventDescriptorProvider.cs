namespace Notrelix.Platform.Messaging.Contracts;

public interface IEventDescriptorProvider
{
    EventDescriptor Get(Type eventType);
    EventDescriptor Get(string eventName, int version);
}
