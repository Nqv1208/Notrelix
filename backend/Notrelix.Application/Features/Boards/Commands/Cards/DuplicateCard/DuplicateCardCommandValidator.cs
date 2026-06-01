using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Cards.DuplicateCard;

public class DuplicateCardCommandValidator : AbstractValidator<DuplicateCardCommand>
{
    public DuplicateCardCommandValidator()
    {
    }
}
