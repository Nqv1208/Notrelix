using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands.Boards.CreateBoardBySlug;

public class CreateBoardBySlugCommandValidator : AbstractValidator<CreateBoardBySlugCommand>
{
    public CreateBoardBySlugCommandValidator()
    {
    }
}
