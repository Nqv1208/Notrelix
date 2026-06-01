using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Labels.AddLabelToCard;

public class AddLabelToCardCommandValidator : AbstractValidator<AddLabelToCardCommand>
{
    public AddLabelToCardCommandValidator()
    {
    }
}
