using FluentValidation;

namespace Notrelix.Application.Features.Boards.Board.Commands.MoveCard;

public class MoveCardCommandValidator : AbstractValidator<MoveCardCommand>
{
    public MoveCardCommandValidator()
    {
        RuleFor(v => v.CardId)
            .NotEmpty().WithMessage("CardId is required.");

        RuleFor(v => v.ListId)
            .NotEmpty().WithMessage("ListId is required.");
            
        // Position có thể là số âm hoặc dương tùy thuật toán ở client, nên không có rule strict.
    }
}
