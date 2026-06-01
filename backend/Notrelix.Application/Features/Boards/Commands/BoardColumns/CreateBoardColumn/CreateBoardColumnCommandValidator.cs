using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.BoardColumns.CreateBoardColumn;

public class CreateBoardColumnCommandValidator : AbstractValidator<CreateBoardColumnCommand>
{
    public CreateBoardColumnCommandValidator()
    {
    }
}
