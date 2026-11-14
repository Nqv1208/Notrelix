namespace Notrelix.Domain.Collaboration.Attachments;

public sealed class FileMetadata : ValueObject
{
    public string FileName { get; private set; } = null!;
    public long Size { get; }
    public string ContentType { get; private set; } = null!;
    public string? StorageKey { get; }
    public string? Url { get; }

    /// <summary>
    /// The pixel width and height of an image file, read from its stored header by the upload flow. Null for every other file and
    /// for an image whose header could not be read. Never client-declared. Both are set or both are null.
    /// </summary>
    public int? Width { get; }
    public int? Height { get; }

    /// <summary>
    /// The length of an audio file in milliseconds, and its waveform (up to <c>MaxWaveformPeaks</c> peaks, each 0 to 255). Set only on an
    /// audio attachment that is a voice message. The duration is read from the container header when the header carries it; otherwise it
    /// is the client's hint, clamped to 0..<c>MaxDurationMs</c>, and is advisory. Never used alone for a policy decision.
    /// </summary>
    public int? DurationMs { get; }
    public byte[]? Waveform { get; }

    public const int MaxWaveformPeaks = 64;
    public const int MaxDurationMs = 300_000;

    private FileMetadata() { }
    private FileMetadata(
        string fileName, long size, string contentType, string? storageKey, string? url, int? width, int? height, int? durationMs, byte[]? waveform)
    {
        FileName = fileName;
        Size = size;
        ContentType = contentType;
        StorageKey = storageKey;
        Url = url;
        Width = width;
        Height = height;
        DurationMs = durationMs;
        Waveform = waveform;
    }

    public const int MaxImageSide = 20000;

    public static FileMetadata Create(
        string fileName,
        long size,
        string contentType,
        string? storageKey = null,
        string? url = null,
        int? width = null,
        int? height = null,
        int? durationMs = null,
        byte[]? waveform = null)
    {
        Guard.NotNullOrWhiteSpace(fileName);
        if ((width is null) != (height is null))
            throw new ArgumentException("An image size needs both a width and a height.");
        if (width is { } w && (w is < 1 or > MaxImageSide))
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height is { } h && (h is < 1 or > MaxImageSide))
            throw new ArgumentOutOfRangeException(nameof(height));
        if (durationMs is { } d && (d < 0 || d > MaxDurationMs))
            throw new ArgumentOutOfRangeException(nameof(durationMs));
        if (waveform is { } peaks && (peaks.Length is < 1 or > MaxWaveformPeaks))
            throw new ArgumentOutOfRangeException(nameof(waveform));
        return new FileMetadata(fileName, size, contentType, storageKey, url, width, height, durationMs, waveform);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FileName;
        yield return Size;
        yield return ContentType;
        yield return StorageKey;
        yield return Url;
        yield return Width;
        yield return Height;
        yield return DurationMs;
        yield return Waveform is null ? null : Convert.ToBase64String(Waveform);
    }
}
