
namespace Notrelix.Application.Features.Collaboration.Comments.Commands.UpdateComment;

public class UpdateCommentCommandValidator : AbstractValidator<UpdateCommentCommand>
{
    public UpdateCommentCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEmpty();
        RuleFor(x => x.ContentMd).NotEmpty().MaximumLength(Comment.MaxContentLength);
    }
}
