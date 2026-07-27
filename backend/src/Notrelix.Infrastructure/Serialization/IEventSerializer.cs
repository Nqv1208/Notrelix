namespace Notrelix.Infrastructure.Serialization;

public interface IEventSerializer
{
    ReadOnlyMemory<byte> Serialize<T>(T @event) where T : class;
    T? Deserialize<T>(ReadOnlyMemory<byte> data) where T : class;
    object? Deserialize(ReadOnlyMemory<byte> data, Type targetType);
}
