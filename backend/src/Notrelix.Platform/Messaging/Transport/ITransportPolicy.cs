using Notrelix.Platform.Messaging.Contracts;
using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Transport;

public interface ITransportPolicy
{
    string ResolveTopic(EventDescriptor descriptor, PublishContext context);
}
