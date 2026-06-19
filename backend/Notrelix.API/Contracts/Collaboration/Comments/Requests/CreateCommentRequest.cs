namespace Notrelix.API.Contracts.Collaboration.Comments.Requests;

public record CreateCommentRequest(string ContentMd, Guid? ParentCommentId = null);
