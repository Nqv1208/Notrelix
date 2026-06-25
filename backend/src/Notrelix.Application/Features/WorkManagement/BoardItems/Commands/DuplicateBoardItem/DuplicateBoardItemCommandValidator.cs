using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DuplicateBoardItem;

public class DuplicateBoardItemCommandValidator : AbstractValidator<DuplicateBoardItemCommand>
{
    public DuplicateBoardItemCommandValidator()
    {
        RuleFor(x => x.BoardItemId)
            .NotEmpty();
    }
}
