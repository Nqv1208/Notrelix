using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.RemoveLabelFromBoardItem;

public class RemoveLabelFromCardCommandValidator : AbstractValidator<RemoveLabelFromCardCommand>
{
    public RemoveLabelFromCardCommandValidator()
    {
    }
}
