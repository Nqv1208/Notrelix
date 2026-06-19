using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnlinkPageFromBoardItem;

public class UnlinkPageFromCardCommandValidator : AbstractValidator<UnlinkPageFromBoardItemCommand>
{
    public UnlinkPageFromCardCommandValidator()
    {
    }
}
