using FluentValidation;

namespace Notrelix.Application.Features.Collaboration.Comments.Commands.UpdateComment;

public class UpdateCommentCommandValidator : AbstractValidator<UpdateCommentCommand>
{
    public UpdateCommentCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEmpty();
        RuleFor(x => x.ContentMd).NotEmpty().MaximumLength(50000);
    }
}
