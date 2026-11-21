namespace Notrelix.Application.Common.Storage;

/// <summary>
/// Forward-only read wrapper that keeps a copy of the last <c>capacity</c> bytes that passed through it, while the bytes are still
/// streamed to the reader unchanged. Used to read the last page of an Ogg file for its duration during the one copy to storage.
/// Does not own the source.
/// </summary>
public sealed class TrailingBytesCaptureStream : Stream
{
    private readonly Stream _inner;
    private readonly byte[] _buffer;
    private long _count;

    public TrailingBytesCaptureStream(Stream inner, int capacity)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _inner = inner;
        _buffer = new byte[capacity];
    }

    /// <summary>The last bytes read so far, at most the capacity, in stream order.</summary>
    public ReadOnlySpan<byte> Captured => _count <= _buffer.Length ? _buffer.AsSpan(0, (int)_count) : Ordered();

    private byte[] Ordered()
    {
        var start = (int)(_count % _buffer.Length);
        var result = new byte[_buffer.Length];
        _buffer.AsSpan(start).CopyTo(result);
        _buffer.AsSpan(0, start).CopyTo(result.AsSpan(_buffer.Length - start));
        return result;
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

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = _inner.Read(buffer, offset, count);
        Capture(buffer.AsSpan(offset, read));
        return read;
    }

    public override int Read(Span<byte> buffer)
    {
        var read = _inner.Read(buffer);
        Capture(buffer[..read]);
        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var read = await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
        Capture(buffer.AsSpan(offset, read));
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await _inner.ReadAsync(buffer, cancellationToken);
        Capture(buffer.Span[..read]);
        return read;
    }

    private void Capture(ReadOnlySpan<byte> delivered)
    {
        foreach (var b in delivered)
        {
            _buffer[(int)(_count % _buffer.Length)] = b;
            _count++;
        }
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
