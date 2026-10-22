namespace Notrelix.API.Contracts.Collaboration.Attachments.Requests;

/// <summary>
/// OpenAPI description of the multipart/form-data body of the file upload operation. The endpoint
/// reads the body as a stream and never binds this type, so the file is never buffered in memory
/// or in a temporary file. The part's own Content-Type is ignored: the stored content type is
/// derived from the bytes.
/// </summary>
public sealed class UploadBoardItemAttachmentForm
{
    /// <summary>The file part. Its filename is sanitised server-side and used for display only.</summary>
    public required IFormFile File { get; init; }
}
