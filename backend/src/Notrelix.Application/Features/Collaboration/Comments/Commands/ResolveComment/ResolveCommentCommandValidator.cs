namespace Notrelix.Application.Features.Collaboration.Comments.Commands.ResolveComment;

public class ResolveCommentCommandValidator : AbstractValidator<ResolveCommentCommand>
{
    public ResolveCommentCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEmpty();
    }
}
