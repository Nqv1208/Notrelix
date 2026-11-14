namespace Notrelix.API.Contracts.Chat.Requests;

/// <summary>
/// OpenAPI description of the multipart/form-data body of the message file upload. The endpoint reads the body as a stream and never
/// binds this type, so the file is never buffered in memory or in a temporary file. The part's own Content-Type is ignored: the stored
/// content type is derived from the bytes.
/// </summary>
public sealed class UploadChatAttachmentForm
{
    /// <summary>The file part. Its filename is sanitised server-side and used for display only.</summary>
    public required IFormFile File { get; init; }

    /// <summary>Optional, before the file part: "true" makes the file a voice message (one audio file, within the voice limit).</summary>
    public string? Voice { get; init; }

    /// <summary>Optional, before the file part: the length in milliseconds the browser measured. A hint only; clamped, never trusted for policy.</summary>
    public string? DurationMs { get; init; }

    /// <summary>Optional, before the file part: 1 to 64 comma-separated waveform peaks, each 0 to 255.</summary>
    public string? Waveform { get; init; }
}
