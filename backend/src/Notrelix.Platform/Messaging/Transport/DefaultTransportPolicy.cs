using Notrelix.Platform.Messaging.Contracts;
using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Transport;

public sealed class DefaultTransportPolicy : ITransportPolicy
{
    private readonly ITopicResolver _topicResolver;

    public DefaultTransportPolicy(ITopicResolver topicResolver)
    {
        _topicResolver = topicResolver;
    }

    public string ResolveTopic(EventDescriptor descriptor, PublishContext context)
    {
        return _topicResolver.ResolveTopic(descriptor);
    }
}
