namespace Notrelix.Platform.Messaging.Contracts;

public interface ITopicResolver
{
    string ResolveTopic(EventDescriptor descriptor);
}
