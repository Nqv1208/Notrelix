using global::Notrelix.Application.Common.Models;
using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class LinkPageToCardCommandValidator : AbstractValidator<LinkPageToBoardItemCommand>
{
    public LinkPageToCardCommandValidator()
    {
        RuleFor(v => v.BoardItemId)
            .NotEmpty().WithMessage("BoardItemId is required.");

        RuleFor(v => v.PageId)
            .NotEmpty().WithMessage("PageId is required.");
    }
}
