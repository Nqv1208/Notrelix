namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DuplicateBoardItem;

public class DuplicateBoardItemCommandValidator : AbstractValidator<DuplicateBoardItemCommand>
{
    public DuplicateBoardItemCommandValidator()
    {
        RuleFor(x => x.BoardItemId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .MaximumLength(200)
            .When(x => x.Name is not null);
    }
}
