using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Shared.Commands.Comments.DeleteComment;

public record DeleteCommentCommand(Guid CommentId) : IRequest<Result>;

public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    public DeleteCommentCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteCommentCommand request, CancellationToken ct)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == request.CommentId, ct);
        if (comment is null) throw new NotFoundException(nameof(Comment), request.CommentId);
        comment.SoftDelete(_currentUser.UserId, _dateTimeProvider.UtcNow);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
