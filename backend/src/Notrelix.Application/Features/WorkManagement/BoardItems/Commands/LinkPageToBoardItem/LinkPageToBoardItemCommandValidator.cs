using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.LinkPageToBoardItem;

public class LinkPageToBoardItemCommandValidator : AbstractValidator<LinkPageToBoardItemCommand>
{
    public LinkPageToBoardItemCommandValidator()
    {
        RuleFor(v => v.BoardItemId)
            .NotEmpty().WithMessage("BoardItemId is required.");

        RuleFor(v => v.PageId)
            .NotEmpty().WithMessage("PageId is required.");
    }
}
