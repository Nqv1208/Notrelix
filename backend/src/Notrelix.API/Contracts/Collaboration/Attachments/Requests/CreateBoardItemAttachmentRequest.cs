namespace Notrelix.API.Contracts.Collaboration.Attachments.Requests;

/// <summary>
/// Registers a link. <c>SizeBytes</c>, <c>ContentType</c> and <c>Source</c> are accepted for older clients and ignored: the server stores
/// size 0, content type <c>text/uri-list</c> and source Link for every link.
/// </summary>
public record CreateBoardItemAttachmentRequest(string Filename, string Url, long? SizeBytes, string? ContentType, string? Source);
