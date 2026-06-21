using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItem;

public class UpdateCardCommandValidator : AbstractValidator<UpdateBoardItemCommand>
{
    public UpdateCardCommandValidator()
    {
    }
}
