using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Shared.Commands.Comments.ResolveComment;

public record ResolveCommentCommand(Guid CommentId) : IRequest<Result>;

public class ResolveCommentCommandHandler : IRequestHandler<ResolveCommentCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ResolveCommentCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ResolveCommentCommand request, CancellationToken ct)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == request.CommentId, ct);
        if (comment is null) throw new NotFoundException(nameof(Comment), request.CommentId);
        comment.Resolve(_currentUser.UserId, _dateTimeProvider.UtcNow);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
