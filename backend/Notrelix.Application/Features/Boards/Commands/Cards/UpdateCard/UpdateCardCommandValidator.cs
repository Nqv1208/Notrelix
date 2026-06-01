using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Cards.UpdateCard;

public class UpdateCardCommandValidator : AbstractValidator<UpdateCardCommand>
{
    public UpdateCardCommandValidator()
    {
    }
}
