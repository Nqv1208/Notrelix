using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemStatus;

public class UpdateCardStatusCommandValidator : AbstractValidator<UpdateBoardItemStatusCommand>
{
    public UpdateCardStatusCommandValidator()
    {
    }
}
