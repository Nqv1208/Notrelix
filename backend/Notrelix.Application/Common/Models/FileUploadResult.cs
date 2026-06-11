namespace Notrelix.Application.Common.Models;

public record FileUploadResult(
    string Url,
    string FilePath,
    string ContentType,
    long SizeBytes
);
