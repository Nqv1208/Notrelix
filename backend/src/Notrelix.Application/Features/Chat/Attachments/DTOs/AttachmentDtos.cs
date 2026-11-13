namespace Notrelix.Application.Features.Chat.Attachments.DTOs;

/// <summary>
/// One file on a message as readers see it (UC-CHT-G08, CHT-012): metadata only. <c>DownloadPath</c> is the API path that streams the
/// bytes; each request to it is permission checked (a reader of the space) and the response is never cacheable, so there is no durable
/// public address and nothing to expire or leak. <c>Width</c> and <c>Height</c> are the pixel size of an image, read from its stored
/// header at upload, so a reader can reserve its box before the bytes load; null for any other file and for an image whose header
/// could not be read.
/// </summary>
public sealed record MessageAttachmentDto(
    Guid Id,
    Guid MessageId,
    string FileName,
    long SizeBytes,
    string ContentType,
    Guid UploadedBy,
    DateTimeOffset CreatedAt,
    string DownloadPath,
    int? Width = null,
    int? Height = null);

/// <summary>An opened file of a message. The consumer owns the stream and disposes it.</summary>
public sealed record MessageAttachmentContentDto(Stream Content, string FileName, string ContentType, long SizeBytes);
