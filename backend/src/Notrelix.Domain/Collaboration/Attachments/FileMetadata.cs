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

    private FileMetadata() { }
    private FileMetadata(string fileName, long size, string contentType, string? storageKey, string? url, int? width, int? height)
    {
        FileName = fileName;
        Size = size;
        ContentType = contentType;
        StorageKey = storageKey;
        Url = url;
        Width = width;
        Height = height;
    }

    public const int MaxImageSide = 20000;

    public static FileMetadata Create(
        string fileName, long size, string contentType, string? storageKey = null, string? url = null, int? width = null, int? height = null)
    {
        Guard.NotNullOrWhiteSpace(fileName);
        if ((width is null) != (height is null))
            throw new ArgumentException("An image size needs both a width and a height.");
        if (width is { } w && (w is < 1 or > MaxImageSide))
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height is { } h && (h is < 1 or > MaxImageSide))
            throw new ArgumentOutOfRangeException(nameof(height));
        return new FileMetadata(fileName, size, contentType, storageKey, url, width, height);
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
    }
}
