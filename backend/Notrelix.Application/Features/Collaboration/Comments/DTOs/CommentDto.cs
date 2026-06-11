namespace Notrelix.Application.Features.Shared.Comments.DTOs;

public record CommentDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string? UserAvatar,
    string ContentMd,
    Guid? ParentCommentId,
    bool IsEdited,
    DateTime? ResolvedAt,
    DateTime CreatedAt
);
