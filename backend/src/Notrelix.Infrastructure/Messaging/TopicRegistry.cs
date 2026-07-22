using System.Collections.Concurrent;

namespace Notrelix.Infrastructure.Messaging;

public sealed class TopicRegistry : ITopicRegistry
{
    private readonly ConcurrentDictionary<string, string> _topics = new(StringComparer.OrdinalIgnoreCase);

    public TopicRegistry(IDictionary<string, string> topics)
    {
        foreach (var kvp in topics)
        {
            _topics[kvp.Key] = kvp.Value;
        }
    }

    public string GetTopic(string eventName, int version)
    {
        var key = $"{eventName}:v{version}";
        if (_topics.TryGetValue(key, out var topic))
            return topic;

        return DeriveTopic(eventName, version);
    }

    private static string DeriveTopic(string eventName, int version)
    {
        var parts = eventName.Split('.');
        if (parts.Length >= 2)
        {
            var domain = parts[0];
            var name = parts[^1];
            return $"{domain}.{name}.v{version}";
        }
        return $"{eventName}.v{version}";
    }
}
