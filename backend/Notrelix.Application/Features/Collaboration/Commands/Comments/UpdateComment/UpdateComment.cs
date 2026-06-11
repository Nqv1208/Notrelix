using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Shared.Comments.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Shared.Commands.Comments.UpdateComment;

public record UpdateCommentCommand(Guid CommentId, string ContentMd) : IRequest<Result>;

public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UpdateCommentCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateCommentCommand request, CancellationToken ct)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == request.CommentId, ct);
        if (comment is null) throw new NotFoundException(nameof(Comment), request.CommentId);
        comment.Edit(request.ContentMd);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
