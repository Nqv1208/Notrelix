using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemDueDate;

public class SetCardDueDateCommandValidator : AbstractValidator<SetBoardItemDueDateCommand>
{
    public SetCardDueDateCommandValidator()
    {
    }
}
