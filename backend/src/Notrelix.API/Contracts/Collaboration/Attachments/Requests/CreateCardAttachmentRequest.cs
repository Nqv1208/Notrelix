namespace Notrelix.API.Contracts.Collaboration.Attachments.Requests;

public record CreateCardAttachmentRequest(string Filename, string Url, long? SizeBytes, string? ContentType, string? Source);
