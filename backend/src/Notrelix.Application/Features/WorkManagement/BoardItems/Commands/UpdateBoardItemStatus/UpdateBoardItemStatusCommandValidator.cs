namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemStatus;

public class UpdateBoardItemStatusCommandValidator : AbstractValidator<UpdateBoardItemStatusCommand>
{
    public UpdateBoardItemStatusCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty().MaximumLength(200);
    }
}
