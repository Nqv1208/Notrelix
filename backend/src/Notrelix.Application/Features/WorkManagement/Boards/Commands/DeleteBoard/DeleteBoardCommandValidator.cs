namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.DeleteBoard;

public class DeleteBoardCommandValidator : AbstractValidator<DeleteBoardCommand>
{
    public DeleteBoardCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty();
    }
}
