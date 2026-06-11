using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class SetCardDueDateCommandValidator : AbstractValidator<SetBoardItemDueDateCommand>
{
    public SetCardDueDateCommandValidator()
    {
    }
}
