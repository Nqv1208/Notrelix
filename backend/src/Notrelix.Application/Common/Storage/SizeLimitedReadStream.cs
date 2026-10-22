namespace Notrelix.Application.Common.Storage;

/// <summary>
/// Forward-only read wrapper that counts the bytes it hands out and fails as soon as the
/// total exceeds the cap. The cap is enforced while streaming (never after buffering), so
/// an oversized body aborts the copy to storage after at most one buffer past the limit.
/// </summary>
public sealed class SizeLimitedReadStream : Stream
{
    private readonly Stream _inner;
    private readonly long _maxBytes;

    public SizeLimitedReadStream(Stream inner, long maxBytes)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        _inner = inner;
        _maxBytes = maxBytes;
    }

    /// <summary>Total bytes delivered to the reader so far.</summary>
    public long BytesRead { get; private set; }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => BytesRead;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => Account(_inner.Read(buffer, offset, count));

    public override int Read(Span<byte> buffer) => Account(_inner.Read(buffer));

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        Account(await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken));

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        Account(await _inner.ReadAsync(buffer, cancellationToken));

    private int Account(int read)
    {
        BytesRead += read;
        if (BytesRead > _maxBytes)
        {
            throw new PayloadTooLargeException(_maxBytes);
        }

        return read;
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
