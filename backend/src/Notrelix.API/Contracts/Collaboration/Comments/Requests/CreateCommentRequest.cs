namespace Notrelix.API.Contracts.Collaboration.Comments.Requests;

public record CreateCommentRequest(string ContentMd, Guid? ParentCommentId = null, Guid[]? MentionedUserIds = null, CommentAnchorRequest? Anchor = null);

/// <summary>A quoted range inside one block of the commented page (page comments only; offsets are UTF-16 code units of the block's plain text).</summary>
public record CommentAnchorRequest(Guid BlockId, string Quote, int Start, int End);
