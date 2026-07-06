namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.DeleteBoardField;

public class DeleteBoardFieldCommandValidator : AbstractValidator<DeleteBoardFieldCommand>
{
    public DeleteBoardFieldCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.ColumnId).NotEmpty();
    }
}
