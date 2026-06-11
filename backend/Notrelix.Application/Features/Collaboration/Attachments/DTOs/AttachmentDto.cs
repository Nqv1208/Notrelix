namespace Notrelix.Application.Features.Shared.Attachments.DTOs;

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
    DateTime CreatedAt
);
