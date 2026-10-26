namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ReopenBoardItem;

public class ReopenBoardItemCommandValidator : AbstractValidator<ReopenBoardItemCommand>
{
    public ReopenBoardItemCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
    }
}
