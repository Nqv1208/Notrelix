using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Cards.UpdateCardStatus;

public class UpdateCardStatusCommandValidator : AbstractValidator<UpdateCardStatusCommand>
{
    public UpdateCardStatusCommandValidator()
    {
    }
}
