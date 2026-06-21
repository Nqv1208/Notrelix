using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.AddLabelToBoardItem;

public class AddLabelToCardCommandValidator : AbstractValidator<AddLabelToCardCommand>
{
    public AddLabelToCardCommandValidator()
    {
    }
}
