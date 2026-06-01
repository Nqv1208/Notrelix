using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.BoardColumns.DeleteBoardColumn;

public class DeleteBoardColumnCommandValidator : AbstractValidator<DeleteBoardColumnCommand>
{
    public DeleteBoardColumnCommandValidator()
    {
    }
}
