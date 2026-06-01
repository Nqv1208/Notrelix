using FluentValidation;

namespace Notrelix.Application.Features.Shared.Commands.Comments.CreateComment;

public class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
    }
}
