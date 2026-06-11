using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands.Labels.AddLabelToBoardItem;

public class AddLabelToCardCommandValidator : AbstractValidator<AddLabelToCardCommand>
{
    public AddLabelToCardCommandValidator()
    {
    }
}
