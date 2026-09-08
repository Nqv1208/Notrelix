namespace Notrelix.API.Contracts.Collaboration.Comments.Requests;

public record CreateCommentRequest(string ContentMd, Guid? ParentCommentId = null, Guid[]? MentionedUserIds = null);
