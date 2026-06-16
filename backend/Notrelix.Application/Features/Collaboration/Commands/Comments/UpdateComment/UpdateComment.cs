using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Shared.Commands.Comments.UpdateComment;

public record UpdateCommentCommand(Guid CommentId, string ContentMd) : IRequest<Result>;

public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    public UpdateCommentCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateCommentCommand request, CancellationToken ct)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == request.CommentId, ct);
        if (comment is null) throw new NotFoundException(nameof(Comment), request.CommentId);
        comment.UpdateContent(request.ContentMd, _currentUser.UserId, _dateTimeProvider.UtcNow);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
