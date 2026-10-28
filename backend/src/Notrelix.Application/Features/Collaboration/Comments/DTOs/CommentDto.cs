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
    IReadOnlyList<ReactionSummaryDto> Reactions,
    CommentAnchorDto? Anchor = null
);

/// <summary>One emoji on a comment: how many people chose it, whether the caller did, and who.</summary>
public record ReactionSummaryDto(string Emoji, int Count, bool ReactedByMe, IReadOnlyList<ReactionUserDto> Users);

public record ReactionUserDto(Guid UserId, string Name);

/// <summary>The quoted text range of a page block a comment points at; it may be stale once the block changed.</summary>
public record CommentAnchorDto(Guid BlockId, string Quote, int Start, int End);
