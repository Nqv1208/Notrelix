using global::Notrelix.Application.Common.Models;
using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Cards.LinkPageToCard;

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
