namespace Notrelix.Application.Features.Collaboration.Attachments.DTOs;

/// <summary>
/// Attachment read model. <c>Kind</c> is "File" for an uploaded file (bytes in object storage,
/// <c>Url</c> is empty and the content is fetched through the authorised content operation) and
/// "Link" for an external link (<c>Url</c> is the external address). <c>Source</c> keeps its
/// established meaning: the attachment type name.
/// </summary>
public record AttachmentDto(
    Guid Id,
    Guid ResourceId,
    string Filename,
    string Url,
    long SizeBytes,
    string ContentType,
    string Source,
    Guid UploadedBy,
    string? UploadedByName,
    DateTime CreatedAt,
    string Kind = AttachmentDto.LinkKind)
{
    public const string FileKind = "File";
    public const string LinkKind = "Link";

    public static AttachmentDto From(Attachment attachment, string? uploadedByName = null) => new(
        attachment.Id,
        attachment.Target.ResourceId,
        attachment.Metadata.FileName,
        attachment.IsStoredFile ? "" : attachment.Metadata.Url ?? "",
        attachment.Metadata.Size,
        attachment.Metadata.ContentType,
        attachment.Type.ToString(),
        attachment.CreatedBy!.Value,
        uploadedByName,
        attachment.CreatedAt.DateTime,
        attachment.IsStoredFile ? FileKind : LinkKind);
}

/// <summary>
/// An opened attachment object. The consumer owns the stream and disposes it.
/// </summary>
public sealed record AttachmentContentDto(
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes);
