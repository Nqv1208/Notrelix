using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Boards.UnarchiveBoard;

public class UnarchiveBoardCommandValidator : AbstractValidator<UnarchiveBoardCommand>
{
    public UnarchiveBoardCommandValidator()
    {
    }
}
