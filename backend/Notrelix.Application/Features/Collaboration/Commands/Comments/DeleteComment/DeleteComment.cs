using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Shared.Comments.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Shared.Commands.Comments.DeleteComment;

public record DeleteCommentCommand(Guid CommentId) : IRequest<Result>;

public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public DeleteCommentCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteCommentCommand request, CancellationToken ct)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == request.CommentId, ct);
        if (comment is null) throw new NotFoundException(nameof(Comment), request.CommentId);
        comment.SoftDelete();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
