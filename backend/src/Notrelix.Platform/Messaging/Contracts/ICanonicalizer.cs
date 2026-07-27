namespace Notrelix.Platform.Messaging.Contracts;

public interface ICanonicalizer
{
    ReadOnlyMemory<byte> Canonicalize(ReadOnlyMemory<byte> data);
}
