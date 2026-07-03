namespace Notrelix.API.Contracts.Collaboration.Attachments.Requests;

public record CreateBoardItemAttachmentRequest(string Filename, string Url, long? SizeBytes, string? ContentType, string? Source);
