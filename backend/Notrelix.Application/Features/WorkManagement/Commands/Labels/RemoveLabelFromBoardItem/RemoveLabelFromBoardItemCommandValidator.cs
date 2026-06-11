using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands.Labels.RemoveLabelFromBoardItem;

public class RemoveLabelFromCardCommandValidator : AbstractValidator<RemoveLabelFromCardCommand>
{
    public RemoveLabelFromCardCommandValidator()
    {
    }
}
