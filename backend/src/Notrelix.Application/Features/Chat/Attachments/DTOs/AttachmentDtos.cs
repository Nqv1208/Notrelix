namespace Notrelix.Application.Features.Chat.Attachments.DTOs;

/// <summary>
/// One file on a message as readers see it (UC-CHT-G08, CHT-012): metadata only. <c>DownloadPath</c> is the API path that streams the
/// bytes; each request to it is permission checked (a reader of the space) and the response is never cacheable, so there is no durable
/// public address and nothing to expire or leak. <c>Width</c> and <c>Height</c> are the pixel size of an image, read from its stored
/// header at upload, so a reader can reserve its box before the bytes load; null for any other file and for an image whose header
/// could not be read. <c>DurationMs</c> and <c>Waveform</c> are set on an audio file that is a voice message (UC-CHT-I04): the duration is
/// read from the header when it carries one, else the client's clamped hint; the waveform is the client's peaks, each 0 to 255.
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
    int? Height = null,
    int? DurationMs = null,
    int[]? Waveform = null);

/// <summary>An opened file of a message. The consumer owns the stream and disposes it.</summary>
public sealed record MessageAttachmentContentDto(Stream Content, string FileName, string ContentType, long SizeBytes);

/// <summary>
/// The answer to a two-step upload create (UC-CHT-G08, two-step): the pending upload id, the pre-signed PUT the client sends the bytes to
/// (with <see cref="ContentType"/> and <see cref="ContentLength"/> bound by the signature), the upload window and the commit path that
/// finishes it. The URL is a secret for its window only: it is never logged and is not a download address.
/// </summary>
public sealed record MessageAttachmentUploadDto(
    Guid UploadId,
    string Method,
    string UploadUrl,
    string ContentType,
    long ContentLength,
    DateTimeOffset ExpiresAt,
    string CommitPath);

/// <summary>A pre-signed download of one file. Valid until <see cref="ExpiresAt"/>; authorised at the moment it was issued.</summary>
public sealed record MessageAttachmentUrlDto(Guid AttachmentId, string FileName, string ContentType, long SizeBytes, string Url, DateTimeOffset ExpiresAt);
