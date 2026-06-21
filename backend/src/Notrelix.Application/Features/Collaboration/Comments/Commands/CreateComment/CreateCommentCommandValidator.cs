using FluentValidation;

namespace Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;

public class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
    }
}
