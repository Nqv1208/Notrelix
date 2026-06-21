using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardBySlug;

public class CreateBoardBySlugCommandValidator : AbstractValidator<CreateBoardBySlugCommand>
{
    public CreateBoardBySlugCommandValidator()
    {
    }
}
