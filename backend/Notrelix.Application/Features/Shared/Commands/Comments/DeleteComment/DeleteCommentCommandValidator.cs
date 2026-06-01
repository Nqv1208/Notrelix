using FluentValidation;

namespace Notrelix.Application.Features.Shared.Commands.Comments.DeleteComment;

public class DeleteCommentCommandValidator : AbstractValidator<DeleteCommentCommand>
{
    public DeleteCommentCommandValidator()
    {
    }
}
