namespace Notrelix.Application.Features.Collaboration.Comments.DTOs;

public record CommentDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string? UserAvatar,
    string ContentMd,
    Guid? ParentCommentId,
    bool IsEdited,
    DateTime? ResolvedAt,
    DateTime CreatedAt,
    CommentAnchorDto? Anchor = null
);

/// <summary>The quoted text range of a page block a comment points at; it may be stale once the block changed.</summary>
public record CommentAnchorDto(Guid BlockId, string Quote, int Start, int End);
