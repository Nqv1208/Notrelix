namespace Notrelix.Platform.Messaging.Contracts;

public interface IEventSerializer
{
    ReadOnlyMemory<byte> Serialize<T>(T @event) where T : class;
    ReadOnlyMemory<byte> Serialize(object @event, Type type);
    T? Deserialize<T>(ReadOnlyMemory<byte> data) where T : class;
    object? Deserialize(ReadOnlyMemory<byte> data, Type targetType);
}
