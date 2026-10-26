namespace Notrelix.Application.Features.Collaboration.Comments.DTOs;

/// <summary>A quoted range of one block of the commented page; offsets count UTF-16 code units of the block's plain text.</summary>
public record CommentAnchorInput(Guid BlockId, string Quote, int Start, int End);
