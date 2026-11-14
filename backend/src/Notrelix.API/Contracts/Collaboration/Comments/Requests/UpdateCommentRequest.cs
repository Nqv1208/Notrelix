namespace Notrelix.API.Contracts.Collaboration.Comments.Requests;

/// <summary>The new text, and the version of the comment the editor read. A stale version is refused with 412.</summary>
public record UpdateCommentRequest(string ContentMd, long? ExpectedVersion);
