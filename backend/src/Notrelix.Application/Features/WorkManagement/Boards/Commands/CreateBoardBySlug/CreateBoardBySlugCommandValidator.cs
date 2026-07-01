namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardBySlug;

public class CreateBoardBySlugCommandValidator : AbstractValidator<CreateBoardBySlugCommand>
{
    public CreateBoardBySlugCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Visibility).IsInEnum();
    }
}
