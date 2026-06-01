using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Cards.SetCardDueDate;

public class SetCardDueDateCommandValidator : AbstractValidator<SetCardDueDateCommand>
{
    public SetCardDueDateCommandValidator()
    {
    }
}
