namespace Notrelix.Application.Common.Events;

public interface ITopicRegistry
{
    string GetTopic(string eventName, int version);
}
