using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Boards.CreateBoardBySlug;

public class CreateBoardBySlugCommandValidator : AbstractValidator<CreateBoardBySlugCommand>
{
    public CreateBoardBySlugCommandValidator()
    {
    }
}
