namespace Notrelix.Application.Common.Storage;

/// <summary>
/// Forward-only read stream that first replays an already-consumed prefix (the bytes read
/// to sniff the content type) and then continues with the remainder of the source stream,
/// so the stored object is byte-identical to the uploaded body. Does not own the source.
/// </summary>
public sealed class PrefixedReadStream : Stream
{
    private readonly ReadOnlyMemory<byte> _prefix;
    private readonly Stream _rest;
    private int _prefixOffset;

    public PrefixedReadStream(ReadOnlyMemory<byte> prefix, Stream rest)
    {
        ArgumentNullException.ThrowIfNull(rest);
        _prefix = prefix;
        _rest = rest;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        if (TryCopyPrefix(buffer, out var copied))
        {
            return copied;
        }

        return _rest.Read(buffer);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (TryCopyPrefix(buffer.Span, out var copied))
        {
            return ValueTask.FromResult(copied);
        }

        return _rest.ReadAsync(buffer, cancellationToken);
    }

    private bool TryCopyPrefix(Span<byte> destination, out int copied)
    {
        var remaining = _prefix.Length - _prefixOffset;
        if (remaining <= 0 || destination.IsEmpty)
        {
            copied = 0;
            return false;
        }

        copied = Math.Min(remaining, destination.Length);
        _prefix.Span.Slice(_prefixOffset, copied).CopyTo(destination);
        _prefixOffset += copied;
        return true;
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
