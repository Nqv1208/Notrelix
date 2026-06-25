using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemDueDate;

public class SetBoardItemDueDateCommandValidator : AbstractValidator<SetBoardItemDueDateCommand>
{
    public SetBoardItemDueDateCommandValidator()
    {
        RuleFor(x => x.BoardItemId)
            .NotEmpty();
    }
}
