using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Labels.RemoveLabelFromCard;

public class RemoveLabelFromCardCommandValidator : AbstractValidator<RemoveLabelFromCardCommand>
{
    public RemoveLabelFromCardCommandValidator()
    {
    }
}
