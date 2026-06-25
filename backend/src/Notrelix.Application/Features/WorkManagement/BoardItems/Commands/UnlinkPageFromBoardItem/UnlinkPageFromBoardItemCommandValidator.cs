using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnlinkPageFromBoardItem;

public class UnlinkPageFromBoardItemCommandValidator : AbstractValidator<UnlinkPageFromBoardItemCommand>
{
    public UnlinkPageFromBoardItemCommandValidator()
    {
        RuleFor(x => x.BoardItemId)
            .NotEmpty();
    }
}
