namespace Notrelix.API.Contracts.Collaboration.Attachments.Requests;

/// <summary>
/// Registers a link on a page. <c>SizeBytes</c> and <c>ContentType</c> are accepted for older clients and ignored: the server stores
/// size 0, content type <c>text/uri-list</c> and source Link for every link.
/// </summary>
public record CreatePageAttachmentRequest(string Filename, string Url, long? SizeBytes, string? ContentType);
