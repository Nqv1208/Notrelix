using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Comments.Commands.UpdateComment;

public record UpdateCommentCommand(Guid CommentId, string ContentMd) : ICommand<Result>, ITransactionalRequest;

public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, Result>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    public UpdateCommentCommandHandler(ICollaborationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
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
        return Result.Success();
    }
}
