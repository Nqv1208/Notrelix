namespace Notrelix.Application.Features.Collaboration.Comments.Commands.ReopenComment;

public class ReopenCommentCommandValidator : AbstractValidator<ReopenCommentCommand>
{
    public ReopenCommentCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEmpty();
    }
}
