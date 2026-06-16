namespace Notrelix.Domain.Common;

public interface IOutboxEvent
{
    string EventType { get; }
}
