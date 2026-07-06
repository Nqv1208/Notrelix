namespace Notrelix.Application.Common.Storage;

public record FileUploadResult(
    string Url,
    string FilePath,
    string ContentType,
    long SizeBytes
);
