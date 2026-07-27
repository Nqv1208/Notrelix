namespace Notrelix.Platform.Messaging.Contracts;

public sealed class DefaultTopicResolver : ITopicResolver
{
    public string ResolveTopic(EventDescriptor descriptor)
    {
        var parts = descriptor.Name.Split('.');
        if (parts.Length >= 2)
        {
            var domain = parts[0];
            var name = parts[^1];
            return $"{domain}.{name}.v{descriptor.Version}";
        }
        return $"{descriptor.Name}.v{descriptor.Version}";
    }
}
