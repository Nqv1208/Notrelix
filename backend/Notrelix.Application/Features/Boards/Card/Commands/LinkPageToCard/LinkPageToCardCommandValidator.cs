using FluentValidation;

namespace Notrelix.Application.Features.Boards.Board.Commands.LinkPageToCard;

public class LinkPageToCardCommandValidator : AbstractValidator<LinkPageToCardCommand>
{
    public LinkPageToCardCommandValidator()
    {
        RuleFor(v => v.CardId)
            .NotEmpty().WithMessage("CardId is required.");

        RuleFor(v => v.PageId)
            .NotEmpty().WithMessage("PageId is required.");
    }
}
