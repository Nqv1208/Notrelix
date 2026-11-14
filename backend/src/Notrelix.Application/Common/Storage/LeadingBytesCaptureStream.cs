namespace Notrelix.Application.Common.Storage;

/// <summary>
/// Forward-only read wrapper that keeps a copy of the first <c>capacity</c> bytes that pass through it, while the bytes are still
/// streamed to the reader unchanged. Used to read an image header (for its pixel size) during the one copy to storage, so no second
/// read of the body happens. Does not own the source.
/// </summary>
public sealed class LeadingBytesCaptureStream : Stream
{
    private readonly Stream _inner;
    private readonly byte[] _buffer;
    private int _captured;

    public LeadingBytesCaptureStream(Stream inner, int capacity)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _inner = inner;
        _buffer = new byte[capacity];
    }

    /// <summary>The first bytes read so far, at most the capacity.</summary>
    public ReadOnlySpan<byte> Captured => _buffer.AsSpan(0, _captured);

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
        return Capture(buffer.AsSpan(offset, read), read);
    }

    public override int Read(Span<byte> buffer)
    {
        var read = _inner.Read(buffer);
        return Capture(buffer[..read], read);
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var read = await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
        Capture(buffer.AsSpan(offset, read), read);
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await _inner.ReadAsync(buffer, cancellationToken);
        Capture(buffer.Span[..read], read);
        return read;
    }

    private int Capture(ReadOnlySpan<byte> delivered, int read)
    {
        var room = _buffer.Length - _captured;
        if (room > 0 && read > 0)
        {
            var take = Math.Min(room, read);
            delivered[..take].CopyTo(_buffer.AsSpan(_captured));
            _captured += take;
        }

        return read;
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
