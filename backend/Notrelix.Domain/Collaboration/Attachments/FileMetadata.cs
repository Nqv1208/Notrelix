using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Attachments;

public sealed class FileMetadata : ValueObject
{
    public string FileName { get; }
    public long Size { get; }
    public string ContentType { get; }
    public string? StorageKey { get; }
    public string? Url { get; }

    private FileMetadata(string fileName, long size, string contentType, string? storageKey, string? url)
    {
        FileName = fileName;
        Size = size;
        ContentType = contentType;
        StorageKey = storageKey;
        Url = url;
    }

    public static FileMetadata Create(string fileName, long size, string contentType, string? storageKey = null, string? url = null)
    {
        Guard.NotNullOrWhiteSpace(fileName);
        return new FileMetadata(fileName, size, contentType, storageKey, url);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FileName;
        yield return Size;
        yield return ContentType;
        yield return StorageKey;
        yield return Url;
    }
}
