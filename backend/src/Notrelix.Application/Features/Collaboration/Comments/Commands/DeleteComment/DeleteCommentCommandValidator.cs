using FluentValidation;

namespace Notrelix.Application.Features.Collaboration.Comments.Commands.DeleteComment;

public class DeleteCommentCommandValidator : AbstractValidator<DeleteCommentCommand>
{
    public DeleteCommentCommandValidator()
    {
    }
}
