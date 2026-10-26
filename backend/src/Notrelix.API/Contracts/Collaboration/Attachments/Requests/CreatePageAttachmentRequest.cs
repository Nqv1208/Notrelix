namespace Notrelix.API.Contracts.Collaboration.Attachments.Requests;

public record CreatePageAttachmentRequest(string Filename, string Url, long? SizeBytes, string? ContentType);
