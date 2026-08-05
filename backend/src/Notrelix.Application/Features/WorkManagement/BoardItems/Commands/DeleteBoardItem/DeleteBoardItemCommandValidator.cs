namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DeleteBoardItem;

public class DeleteBoardItemCommandValidator : AbstractValidator<DeleteBoardItemCommand>
{
    public DeleteBoardItemCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
    }
}
